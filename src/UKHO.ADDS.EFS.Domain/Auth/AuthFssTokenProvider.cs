using Microsoft.Extensions.Options;
using UKHO.ADDS.EFS.Configuration.Authentication;

namespace UKHO.ADDS.EFS.Auth
{
    public class AuthFssTokenProvider : AuthTokenProvider, IAuthFssTokenProvider
    {
        public AuthFssTokenProvider(IOptions<EfsManagedIdentityConfiguration> efsManagedIdentityConfiguration) :
           base(efsManagedIdentityConfiguration)
        {
        }
    }
}
