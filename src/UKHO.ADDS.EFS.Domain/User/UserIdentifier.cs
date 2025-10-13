using System.Diagnostics.CodeAnalysis;

namespace UKHO.ADDS.EFS.Domain.User
{
    [ExcludeFromCodeCoverage]
    public class UserIdentifier
    {
        public string UserIdentity { get; set; }

        public UserIdentifier(string userIdentity)
        {
            UserIdentity = userIdentity ?? string.Empty;
        }
    }
}
