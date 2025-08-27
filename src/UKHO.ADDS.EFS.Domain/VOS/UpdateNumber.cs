using UKHO.ADDS.EFS.Exceptions;
using Vogen;

namespace UKHO.ADDS.EFS.VOS
{
    [ValueObject<int>(Conversions.SystemTextJson, typeof(ValidationException))]
    [Instance("NotSet", 0)]
    public partial struct UpdateNumber
    {
        private static Validation Validate(int input)
        {
            if (input >= 0)
            {
                return Validation.Ok;
            }

            return Validation.Invalid($"{nameof(UpdateNumber)} must be >= 0");
        }
    }
}
