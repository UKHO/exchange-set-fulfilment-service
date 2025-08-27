using Vogen;

namespace UKHO.ADDS.EFS.Implementation
{
    [ValueObject<string>(Conversions.SystemTextJson)]
    [Instance("None", "")]
    public partial struct CorrelationId
    {
    }
}
