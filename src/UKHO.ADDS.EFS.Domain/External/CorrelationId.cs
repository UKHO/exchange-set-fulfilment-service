using Vogen;

namespace UKHO.ADDS.EFS.External
{
    [ValueObject<string>(Conversions.SystemTextJson)]
    [Instance("None", "")]
    public partial struct CorrelationId
    {
    }
}
