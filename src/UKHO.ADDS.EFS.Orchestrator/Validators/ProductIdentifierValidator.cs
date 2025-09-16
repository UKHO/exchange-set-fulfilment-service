using UKHO.ADDS.EFS.Domain.Products;

namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for productIdentifier property
/// </summary>
internal static class ProductIdentifierValidator
{
    public const string VALIDATION_MESSAGE = "productIdentifier must be exactly 4 characters: start with 'S' or 's' followed by three digits, with no spaces or extra characters";

    /// <summary>
    /// Validates the productIdentifier with an optional timeout.
    /// </summary>
        public static bool IsValid(string? productIdentifier)
        {
            if (string.IsNullOrEmpty(productIdentifier))
            {
                return true;
            }

            // Exclude S57 if present in DataStandardProductType enum
            if (Enum.TryParse<DataStandardProductType>(productIdentifier, true, out var productType))
            {
                return productType != DataStandardProductType.S57;
            }

            return false;
        }
}
