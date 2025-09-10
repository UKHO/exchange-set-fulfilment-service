using System.ComponentModel.DataAnnotations;
using Vogen;

namespace UKHO.ADDS.EFS.Domain.Messages
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
