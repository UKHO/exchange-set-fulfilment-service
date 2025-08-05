using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.ADDS.EFS.Configuration.Authentication;

namespace UKHO.ADDS.EFS.Auth
{
    public class AuthFssTokenProvider : AuthTokenProvider, IAuthFssTokenProvider
    {
        public AuthFssTokenProvider(
            IOptions<EfsManagedIdentityConfiguration> efsManagedIdentityConfiguration,
            IDistributedCache cache,
            ILogger<AuthFssTokenProvider> logger) :
           base(efsManagedIdentityConfiguration, cache, logger)
        {
        }
    }
}
