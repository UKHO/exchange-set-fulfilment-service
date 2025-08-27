using UKHO.ADDS.EFS.Exceptions;
using Vogen;

namespace UKHO.ADDS.EFS.VOS
{
    [ValueObject<int>(Conversions.SystemTextJson, typeof(ValidationException))]
    [Instance("None", 0)]
    public partial struct ProductCount
    {

        private static Validation Validate(int input)
        {
            if (input >= 0)
            {
                return Validation.Ok;
            }

            return Validation.Invalid($"{nameof(ProductCount)} must be >= 0");
        }
    }
}
