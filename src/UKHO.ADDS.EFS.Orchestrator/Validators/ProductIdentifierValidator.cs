using UKHO.ADDS.EFS.Domain.Products;

namespace UKHO.ADDS.EFS.Orchestrator.Validators
{
    /// <summary>
    /// Validator for productIdentifier property
    /// </summary>
    internal static class ProductIdentifierValidator
    {
        public const string ValidationMessage = "productIdentifier must be exactly 4 characters: start with 'S' or 's' followed by three digits, with no spaces or extra characters";

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
                if (upperProductIdentifier.StartsWith("S") && productType != DataStandardProductType.S57)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
