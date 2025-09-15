using System.ComponentModel.DataAnnotations;
using Vogen;

namespace UKHO.ADDS.EFS.Domain.Products
{
    [ValueObject<int>(Conversions.SystemTextJson, typeof(ValidationException))]
    [Instance("NotRequired", 0)]
    [Instance("NotSet", 0)]
    public partial struct EditionNumber
    {
        internal static Validation Validate(int input)
        {
            if (input >= 1)
            {
                return Validation.Ok;
            }

            return Validation.Invalid($"{nameof(EditionNumber)} must be a positive integer.");
        }
    }
}
