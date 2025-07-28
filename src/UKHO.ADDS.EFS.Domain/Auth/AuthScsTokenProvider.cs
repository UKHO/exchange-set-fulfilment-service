using Microsoft.Extensions.Options;
using UKHO.ADDS.EFS.Configuration.Authentication;

namespace UKHO.ADDS.EFS.Auth
{
    public class AuthScsTokenProvider : AuthTokenProvider, IAuthScsTokenProvider
    {
        public AuthScsTokenProvider(IOptions<EfsManagedIdentityConfiguration> efsManagedIdentityConfiguration) :
           base(efsManagedIdentityConfiguration)
        {
        }
    }
}
