using System.Diagnostics.CodeAnalysis;

namespace UKHO.ADDS.EFS.Domain.User
{
    [ExcludeFromCodeCoverage]
    public class UserIdentifier
    {
        public required string Identity { get; init; }
    }
}
