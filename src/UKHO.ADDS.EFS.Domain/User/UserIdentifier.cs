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
            UserIdentity = Convert.ToString(httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value) ?? string.Empty;
        }
    }
}
