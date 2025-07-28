using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;
using UKHO.ADDS.EFS.Configuration.Authentication;

namespace UKHO.ADDS.EFS.Auth
{
    public class AuthTokenProvider
    {
        private readonly IOptions<EfsManagedIdentityConfiguration> _efsManagedIdentityConfiguration;

        public AuthTokenProvider(IOptions<EfsManagedIdentityConfiguration> efsManagedIdentityConfiguration)
        {
            _efsManagedIdentityConfiguration = efsManagedIdentityConfiguration ?? throw new ArgumentNullException(nameof(efsManagedIdentityConfiguration));
        }

        public async Task<string> GetManagedIdentityAuthAsync(string resource)
        {
            var newAccessToken = await GetNewAuthToken(resource);

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
    }
}
