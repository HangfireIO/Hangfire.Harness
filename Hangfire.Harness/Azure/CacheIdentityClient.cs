// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

namespace AzureCredentialsSample
{
    /// <summary>
    /// Acquires tokens from Microsoft Entra ID for authenticating connections to Azure Cache for Redis.
    /// </summary>
    internal class CacheIdentityClient
    {
        private readonly Func<CancellationToken, Task<TokenResult>> _getToken;

        private CacheIdentityClient(Func<CancellationToken, ValueTask<AccessToken>> getToken)
            => _getToken = async ct => new TokenResult(await getToken(ct).ConfigureAwait(false));

        private CacheIdentityClient(Func<CancellationToken, Task<AuthenticationResult>> getToken)
            => _getToken = async ct => new TokenResult(await getToken(ct).ConfigureAwait(false));

        public static CacheIdentityClient GetIdentityClient(AzureCacheOptions azureCacheOptions)
        {
            if (azureCacheOptions.TokenCredential != null) // DefaultAzureCredential (or other TokenCredential)
            {
                return CreateForTokenCredential(azureCacheOptions.TokenCredential, azureCacheOptions.Scope);
            }
            else if (azureCacheOptions.ServicePrincipalTenantId != null || azureCacheOptions.ServicePrincipalSecret != null || azureCacheOptions.ServicePrincipalCertificate != null) // Service Principal
            {
                if (azureCacheOptions.ClientId is null || azureCacheOptions.ServicePrincipalTenantId is null)
                {
                    throw new ArgumentException($"To use a service principal, {nameof(azureCacheOptions.ClientId)} and {nameof(azureCacheOptions.ServicePrincipalTenantId)} must be specified");
                }

                if (azureCacheOptions.ServicePrincipalSecret is null && azureCacheOptions.ServicePrincipalCertificate is null)
                {
                    throw new ArgumentException($"To use a service principal, {nameof(azureCacheOptions.ServicePrincipalSecret)} or {nameof(azureCacheOptions.ServicePrincipalCertificate)} must be specified");
                }

                return CreateForServicePrincipal(azureCacheOptions);
            }
            else // Managed identity
            {
                return CreateForManagedIdentity(azureCacheOptions);
            }
        }

        private static CacheIdentityClient CreateForManagedIdentity(AzureCacheOptions options)
        {
            var clientApp = ManagedIdentityApplicationBuilder.Create(
                    options.ClientId is null ?
                        ManagedIdentityId.SystemAssigned
                        : Guid.TryParse(options.ClientId, out _) ?
                            ManagedIdentityId.WithUserAssignedClientId(options.ClientId)
                            : ManagedIdentityId.WithUserAssignedResourceId(options.ClientId))
                .Build();

            return new CacheIdentityClient(getToken: ct => clientApp.AcquireTokenForManagedIdentity(options.Scope).ExecuteAsync(ct));
        }

        private static CacheIdentityClient CreateForServicePrincipal(AzureCacheOptions options)
        {
            var clientApp = ConfidentialClientApplicationBuilder.Create(options.ClientId)
                .WithCloudAuthority(options)
                .WithCredentials(options)
                .Build();

            return new CacheIdentityClient(getToken: ct => clientApp.AcquireTokenForClient(new[] { options.Scope }).ExecuteAsync(ct));
        }

        private static CacheIdentityClient CreateForTokenCredential(TokenCredential tokenCredential, string scope)
        {
            var tokenRequestContext = new TokenRequestContext(new[] { scope });

            return new CacheIdentityClient(getToken: ct => tokenCredential.GetTokenAsync(tokenRequestContext, ct));
        }

        public async Task<TokenResult> GetTokenAsync(CancellationToken cancellationToken) => await _getToken.Invoke(cancellationToken).ConfigureAwait(false);
    }

    internal static class ConfidentialClientApplicationBuilderExtensions
    {
        internal static ConfidentialClientApplicationBuilder WithCloudAuthority(this ConfidentialClientApplicationBuilder builder, AzureCacheOptions options)
            => options.CloudUri is null ?
                builder.WithAuthority(options.Cloud, options.ServicePrincipalTenantId)
                : builder.WithAuthority(options.CloudUri, options.ServicePrincipalTenantId);

        internal static ConfidentialClientApplicationBuilder WithCredentials(this ConfidentialClientApplicationBuilder builder, AzureCacheOptions options)
            => options.ServicePrincipalCertificate is null ?
                builder.WithClientSecret(options.ServicePrincipalSecret)
                : builder.WithCertificate(options.ServicePrincipalCertificate, options.SendX5C);

    }
}