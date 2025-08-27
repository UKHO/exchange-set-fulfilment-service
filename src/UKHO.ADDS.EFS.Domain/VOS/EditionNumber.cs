using UKHO.ADDS.EFS.Exceptions;
using Vogen;

namespace UKHO.ADDS.EFS.VOS
{
    [ValueObject<int>(Conversions.SystemTextJson, typeof(ValidationException))]
    [Instance("NotRequired", 0)]
    public partial struct EditionNumber
    {
        private static Validation Validate(int input)
        {
            if (input >= 0)
            {
                return Validation.Ok;
            }

            return Validation.Invalid($"{nameof(EditionNumber)} must be >= 0");
        }
    }
}
