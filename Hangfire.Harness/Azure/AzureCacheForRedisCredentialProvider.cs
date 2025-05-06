// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Pro.Redis;

namespace AzureCredentialsSample
{
    public sealed class AzureCacheForRedisCredentialProvider : IRedisCredentialProvider
    {
        private readonly CacheIdentityClient _identityClient;

        public AzureCacheForRedisCredentialProvider(AzureCacheOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _identityClient = CacheIdentityClient.GetIdentityClient(options);
        }

        public AzureCacheOptions Options { get; }

        public async Task<IRedisAccessToken> AcquireTokenAsync(CancellationToken cancellationToken)
        {
            Exception lastException = null;
            for (var attemptCount = 0; attemptCount < Options.MaxTokenRefreshAttempts; ++attemptCount)
            {
                try
                {
                    return await _identityClient.GetTokenAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                await Task.Delay(Options.TokenRefreshBackoff(attemptCount, lastException), cancellationToken).ConfigureAwait(false);
            }

            // If we get here, we never successfully acquired a token
            if (lastException != null)
            {
                throw lastException;
            }

            throw new InvalidOperationException("Was unable to acquire a token");
        }

        public Task<string> GetUserNameFromToken(IRedisAccessToken token, CancellationToken cancellationToken)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Options.GetUserName(token.Token));
        }

        public async Task<IRedisAccessToken> TryRefreshTokenAsync(IRedisAccessToken oldToken, CancellationToken cancellationToken)
        {
            if (oldToken == null) throw new ArgumentNullException(nameof(oldToken));
            cancellationToken.ThrowIfCancellationRequested();

            var oldTokenTyped = (TokenResult)oldToken;
            var nowUtc = DateTime.UtcNow;

            if ((oldTokenTyped.ExpiresOnUtc - oldTokenTyped.AcquiredOnUtc) <= TimeSpan.Zero || // Current expiry is not valid
                (oldTokenTyped.ExpiresOnUtc - nowUtc) < (Options.TokenHeartbeatInterval + Options.TokenHeartbeatInterval) || // Within two heartbeats of expiration
                Options.ShouldTokenBeRefreshed(nowUtc, oldTokenTyped.AcquiredOnUtc, oldTokenTyped.ExpiresOnUtc)) // Token is due for refresh
            {
                var leeway = TimeSpan.FromSeconds(30); // Sometimes the updated token may actually have an expiry a few seconds shorter than the original
                var newToken = (TokenResult)await AcquireTokenAsync(cancellationToken);

                if (newToken.ExpiresOnUtc >= oldTokenTyped.ExpiresOnUtc.Subtract(leeway))
                {
                    return newToken;
                }
            }

            return null;
        }

        TimeSpan IRedisCredentialProvider.HeartbeatInterval => Options.TokenHeartbeatInterval;
    }
}