using System.ComponentModel.DataAnnotations;
using Vogen;

namespace UKHO.ADDS.EFS.Domain.Jobs
{
    [ValueObject<string>(Conversions.SystemTextJson, typeof(ValidationException))]
    [Instance("None", "")]
    public partial struct BatchId
    {

        private static Validation Validate(string input)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                return Validation.Ok;
            }

            return Validation.Invalid($"{nameof(BatchId)} must not be empty or whitespace");
        }
    }
}
