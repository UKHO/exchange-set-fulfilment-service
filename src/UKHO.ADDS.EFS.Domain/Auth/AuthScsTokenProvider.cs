using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.ADDS.EFS.Configuration.Authentication;

namespace UKHO.ADDS.EFS.Auth
{
    public class AuthScsTokenProvider : AuthTokenProvider, IAuthScsTokenProvider
    {
        public AuthScsTokenProvider(
            IOptions<EfsManagedIdentityConfiguration> efsManagedIdentityConfiguration,
            IDistributedCache cache,
            ILogger<AuthScsTokenProvider> logger) :
           base(efsManagedIdentityConfiguration, cache, logger)
        {
        }
    }
}
