using System.ComponentModel.DataAnnotations;
using Vogen;

namespace UKHO.ADDS.EFS.Domain.Files
{
    [ValueObject<long>(Conversions.SystemTextJson, typeof(ValidationException))]
    [Instance("Zero", 0)]
    public partial class FileSize
    {
        private static Validation Validate(long input)
        {
            if (input >= 0)
            {
                return Validation.Ok;
            }

            return Validation.Invalid($"{nameof(FileSize)} must be >= 0");
        }
    }
}
