using System.ComponentModel.DataAnnotations;
using Vogen;

namespace UKHO.ADDS.EFS.Domain.External
{
    [ValueObject<string>(Conversions.SystemTextJson, typeof(ValidationException))]
    [Instance("SalesCatalogueService", "SCS")]
    [Instance("FileShareService", "FSS")]
    [Instance("NotDefined", "None")]
    public partial struct ExternalServiceName
    {
        private static Validation Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Validation.Invalid($"{nameof(ExternalServiceName)} cannot be null or empty");
            }

            if (value == "SCS" || value == "FSS" || value == "None")
            {
                return Validation.Ok;
            }

            return Validation.Invalid($"'{value}' is not a valid {nameof(ExternalServiceName)}. Valid values are 'SCS', 'FSS' and 'None'");
        }
    }
}
