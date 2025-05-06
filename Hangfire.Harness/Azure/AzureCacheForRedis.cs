// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Identity.Client;

namespace AzureCredentialsSample
{
    /// <summary>
    /// An extension for StackExchange.Redis for configuring connections to Azure Cache for Redis resources.
    /// </summary>
    public static class AzureCacheForRedis
    {
        /// <summary>
        /// Configures a Redis connection authenticated using a system-assigned managed identity.
        /// </summary>
        /// <exception cref="MsalServiceException">When the token source is not supported or identified incorrectly.</exception>
        /// <exception cref="HttpRequestException">Unable to contact the identity service to acquire a token.</exception>
        public static AzureCacheForRedisCredentialProvider ConfigureForAzureWithSystemAssignedManagedIdentity()
            => new AzureCacheForRedisCredentialProvider(
                new AzureCacheOptions());

        /// <summary>
        /// Configures a Redis connection authenticated using a user-assigned managed identity.
        /// </summary>
        /// <param name="clientId">Client ID or resource ID of the user-assigned managed identity.</param>
        /// <exception cref="MsalServiceException">When the token source is not supported or identified incorrectly.</exception>
        /// <exception cref="HttpRequestException">Unable to contact the identity service to acquire a token.</exception>
        public static AzureCacheForRedisCredentialProvider ConfigureForAzureWithUserAssignedManagedIdentity(string clientId)
            => new AzureCacheForRedisCredentialProvider(
                new AzureCacheOptions()
                {
                    ClientId = clientId,
                });

        /// <summary>
        /// Configures a Redis connection authenticated using a service principal.
        /// NOTE: Service principal authentication should only be used in scenarios where managed identity CANNOT be used.
        /// </summary>
        /// <param name="clientId">Client ID of the service principal.</param>
        /// <param name="tenantId">Tenant ID of the service principal.</param>
        /// <param name="secret">Service principal secret. Either <paramref name="secret"/> or <paramref name="certificate"/> must be provided</param>
        /// <param name="certificate">Service principal certificate. Either <paramref name="certificate"/> or <paramref name="secret"/> must be provided.</param>
        /// <param name="cloud">Optional. Provide a value to use an Azure cloud other than the Public cloud.</param>
        /// <param name="cloudUri">Optional. Provide a value to use an Azure cloud not included in <see cref="AzureCloudInstance"/>. URI format will be similar to <c>https://login.microsoftonline.com)</c></param>
        /// <exception cref="MsalServiceException">When the token source is not supported or identified incorrectly.</exception>
        /// <exception cref="HttpRequestException">Unable to contact the identity service to acquire a token.</exception>
        public static AzureCacheForRedisCredentialProvider ConfigureForAzureWithServicePrincipal(string clientId, string tenantId, string secret = null, X509Certificate2 certificate = null, AzureCloudInstance cloud = AzureCloudInstance.AzurePublic, string cloudUri = null)
            => new AzureCacheForRedisCredentialProvider(
                new AzureCacheOptions()
                {
                    ClientId = clientId,
                    ServicePrincipalTenantId = tenantId,
                    ServicePrincipalSecret = secret,
                    ServicePrincipalCertificate = certificate,
                    Cloud = cloud,
                    CloudUri = cloudUri,
                });

        /// <summary>
        /// Configures a Redis connection authenticated using a TokenCredential.
        /// </summary>
        /// <param name="tokenCredential">The TokenCredential to be used.</param>
        public static AzureCacheForRedisCredentialProvider ConfigureForAzureWithTokenCredential(TokenCredential tokenCredential)
            => new AzureCacheForRedisCredentialProvider(
                new AzureCacheOptions()
                {
                    TokenCredential = tokenCredential
                });
    }
}
