using UKHO.ADDS.EFS.Exceptions;
using Vogen;

namespace UKHO.ADDS.EFS.VOS
{
    [ValueObject<string>(Conversions.SystemTextJson, typeof(ValidationException))]
    public partial struct JobId
    {
        private static Validation Validate(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                return Validation.Ok;
            }

            return Validation.Invalid($"{nameof(JobId)} cannot be null or empty");
        }

        private static string NormalizeInput(string input)
        {
            return input.Trim();
        }

    }
}
