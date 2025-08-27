using UKHO.ADDS.EFS.Exceptions;
using Vogen;

namespace UKHO.ADDS.EFS.VOS
{
    [ValueObject<int>(Conversions.SystemTextJson, typeof(ValidationException))]
    public partial struct MessageVersion
    {
        private static Validation Validate(int input)
        {
            if (input >= 1)
            {
                return Validation.Ok;
            }

            return Validation.Invalid($"{nameof(MessageVersion)} must be >= 1");
        }
    }
}
