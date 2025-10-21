using System.ComponentModel.DataAnnotations;
using Vogen;

namespace UKHO.ADDS.EFS.Domain.Products
{
    [ValueObject<string>(Conversions.SystemTextJson, typeof(ValidationException))]
    [Instance("SalesCatalogueService", "SCS")]
    [Instance("FileShareService", "FSS")]
    public partial struct ServiceNameType
    {
        private static Validation Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Validation.Invalid("ServiceNameType cannot be null or empty");
            }

            if (value == "SCS" || value == "FSS")
            {
                return Validation.Ok;
            }

            return Validation.Invalid($"'{value}' is not a valid ServiceNameType. Valid values are 'SCS' and 'FSS'");
        }

        private static string NormalizePrimitive(string input) => input?.Trim().ToUpperInvariant() ?? string.Empty;

        public static readonly ServiceNameType SCS = From("SCS");
        public static readonly ServiceNameType FSS = From("FSS");
    }
}
