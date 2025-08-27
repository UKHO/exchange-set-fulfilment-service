using Vogen;

namespace UKHO.ADDS.EFS.VOS
{
    [ValueObject<string>(Conversions.SystemTextJson)]
    [Instance("None", "")]
    public partial struct BatchId
    {
    }
}
