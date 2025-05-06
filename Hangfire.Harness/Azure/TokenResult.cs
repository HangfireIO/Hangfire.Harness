using System;
using Azure.Core;
using Hangfire.Pro.Redis;
using Microsoft.Identity.Client;

namespace AzureCredentialsSample
{
    /// <summary>
    /// Result from getting a new token for authentication
    /// </summary>
    internal class TokenResult : IRedisAccessToken
    {
        /// <summary>
        /// Token to be used for authentication.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Expiry for the acquired token.
        /// </summary>
        public DateTime ExpiresOnUtc { get; set; }
    
        public DateTime AcquiredOnUtc { get; set; }

        /// <summary>
        /// Creates a TokenResult from an AuthenticationResult.
        /// </summary>
        /// <param name="authenticationResult">An AuthenticationResult from getting a token through the Microsoft Identity Client.</param>
        public TokenResult(AuthenticationResult authenticationResult)
        {
            Token = authenticationResult.AccessToken;
            ExpiresOnUtc = authenticationResult.ExpiresOn.UtcDateTime;
            AcquiredOnUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a TokenResult from an AccessToken.
        /// </summary>
        /// <param name="accessToken">An AccessToken from a TokenCredential.</param>
        public TokenResult(AccessToken accessToken)
        {
            Token = accessToken.Token;
            ExpiresOnUtc = accessToken.ExpiresOn.UtcDateTime;
            AcquiredOnUtc = DateTime.UtcNow;
        }
    }
}