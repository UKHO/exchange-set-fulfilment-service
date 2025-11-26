using UKHO.ADDS.EFS.Domain.Products;

namespace UKHO.ADDS.EFS.Orchestrator.Validators
{
    /// <summary>
    /// Validator for productIdentifier property
    /// </summary>
    internal static class ProductIdentifierValidator
    {
        public const string ValidationMessage = "Invalid product identifier, It must be exactly 4 characters, starting with 'S' or 's' followed by a valid 3-digit product type";

        /// <summary>
        /// Validates the productIdentifier with an optional timeout.
        /// </summary>
        public static bool IsValid(string? productIdentifier)
        {
            if (string.IsNullOrEmpty(productIdentifier))
            {
                return true;
            }

            var upperProductIdentifier = productIdentifier.ToUpper();
            if (Enum.TryParse<DataStandardProductType>(upperProductIdentifier, out var productType))
            {
                // Only allow identifiers that start with 'S' and are not S57
                if (upperProductIdentifier.StartsWith("S", StringComparison.OrdinalIgnoreCase) && upperProductIdentifier.Length == 4 && productType != DataStandardProductType.S57)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
