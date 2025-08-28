using Vogen;

namespace UKHO.ADDS.EFS.Domain.External
{
    [ValueObject<string>(Conversions.SystemTextJson)]
    [Instance("None", "")]
    public partial struct CorrelationId
    {
    }
}
