using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace UKHO.ADDS.EFS.Domain.User
{
    [ExcludeFromCodeCoverage]
    public class UserIdentifier
    {
        public string UserIdentity { get; set; }
        public UserIdentifier(IHttpContextAccessor httpContextAccessor)
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext != null && httpContext.User != null && httpContext.User.Identity != null && httpContext.User.Identity.IsAuthenticated)
            {
                var principal = httpContext.User;
                if (principal != null)
                {
                    UserIdentity = principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ?? string.Empty;
                }
                else
                {
                    UserIdentity = string.Empty;
                }
            }
            else
            {
                UserIdentity = string.Empty;
            }
        }
    }
}
