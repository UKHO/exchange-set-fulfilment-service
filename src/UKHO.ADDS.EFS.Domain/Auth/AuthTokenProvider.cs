using System.Diagnostics.CodeAnalysis;
using System.Text;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using UKHO.ADDS.EFS.Configuration.Authentication;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Auth
{
    [ExcludeFromCodeCoverage] ////Excluded from code coverage as it has ADD interaction
    public class AuthTokenProvider
    {
        private readonly IOptions<EfsManagedIdentityConfiguration> _efsManagedIdentityConfiguration;
        private readonly IDistributedCache _cache;
        private static readonly object _lock = new object();

        public AuthTokenProvider(
            IOptions<EfsManagedIdentityConfiguration> efsManagedIdentityConfiguration,
            IDistributedCache cache)
        {
            _efsManagedIdentityConfiguration = efsManagedIdentityConfiguration ?? throw new ArgumentNullException(nameof(efsManagedIdentityConfiguration));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<string> GetManagedIdentityAuthAsync(string resource)
        {
            var accessToken = GetAuthTokenFromCache(resource);

            if (accessToken != null && accessToken.AccessToken != null && accessToken.ExpiresIn > DateTime.UtcNow)
            {
                return accessToken.AccessToken;
            }

            var newAccessToken = await GetNewAuthToken(resource);
            AddAuthTokenToCache(resource, newAccessToken);

            return newAccessToken.AccessToken;
        }

        private async Task<AccessTokenItem> GetNewAuthToken(string resource)
        {
            var tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = _efsManagedIdentityConfiguration.Value.EfsClientId });
            var accessToken = await tokenCredential.GetTokenAsync(
                new TokenRequestContext(scopes: [resource + "/.default"]) { }
            );

            return new AccessTokenItem
            {
                ExpiresIn = accessToken.ExpiresOn.UtcDateTime,
                AccessToken = accessToken.Token
            };
        }

        private void AddAuthTokenToCache(string key, AccessTokenItem accessTokenItem)
        {
            var tokenExpiryMinutes = accessTokenItem.ExpiresIn.Subtract(DateTime.UtcNow).TotalMinutes;
            var deductTokenExpiryMinutes = _efsManagedIdentityConfiguration.Value.DeductTokenExpiryMinutes < tokenExpiryMinutes ? _efsManagedIdentityConfiguration.Value.DeductTokenExpiryMinutes : 1;
            var options = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(tokenExpiryMinutes - deductTokenExpiryMinutes));
            options.SetAbsoluteExpiration(accessTokenItem.ExpiresIn);

            lock (_lock)
            {
                _cache.SetString(key, JsonCodec.Encode(accessTokenItem), options);
            }
        }

        private AccessTokenItem? GetAuthTokenFromCache(string key)
        {
            var item = _cache.GetString(key);
            if (item != null)
            {
                return JsonCodec.Decode<AccessTokenItem>(item);
            }

            return null;
        }
    }
}
