using System.ComponentModel.DataAnnotations;
using Vogen;

namespace UKHO.ADDS.EFS.Domain.Products
{
    [ValueObject<string>(Conversions.SystemTextJson, typeof(ValidationException))]
    public partial struct ProductName
    {
        public static Validation Validate(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return Validation.Invalid($"{nameof(ProductName)} cannot be null or empty.");
            }

            var span = input.AsSpan();

            // Try S-100 rule: first 3 chars must be digits
            if (TryParseExactlyThreeDigits(span, out var code))
            {
                if (Enum.IsDefined(typeof(DataStandardProductType), code) && (DataStandardProductType)code != DataStandardProductType.S57) // avoid "000"
                {
                    return Validation.Ok;
                }

                return Validation.Invalid($"'{input}' starts with digits '{code:000}' but that is not a valid S-100 product.");
            }

            // Else, check for S-57
            if (span.Length == 8)
            {
                return Validation.Ok;
            }

            return Validation.Invalid($"'{input}' is not valid: it neither starts with a 3-digit S-100 code nor has length 8 for S-57.");
        }

        public DataStandard DataStandard
        {
            get
            {
                var span = MemoryExtensions.AsSpan(Value);

                if (TryParseExactlyThreeDigits(span, out var _))
                {
                    return DataStandard.S100;
                }

                if (span.Length == 8)
                {
                    return DataStandard.S57;
                }

                throw new InvalidOperationException($"Invalid {nameof(ProductName)}: '{Value}'.");
            }
        }

        public DataStandardProduct DataStandardProduct
        {
            get
            {
                var span = MemoryExtensions.AsSpan(Value);

                if (TryParseExactlyThreeDigits(span, out var code))
                {
                    return Products.DataStandardProduct.From(code);
                }

                if (span.Length == 8)
                {
                    return DataStandardProduct.FromEnum(DataStandardProductType.S57);
                }

                throw new InvalidOperationException($"Invalid {nameof(ProductName)}: '{Value}'.");
            }
        }

        internal static bool TryParseExactlyThreeDigits(ReadOnlySpan<char> span, out int value)
        {
            value = 0;

            if (span.Length < 3)
            {
                return false;
            }

            var d0 = span[0] - '0';
            var d1 = span[1] - '0';
            var d2 = span[2] - '0';

            if ((uint)d0 > 9U || (uint)d1 > 9U || (uint)d2 > 9U)
            {
                return false;
            }

            value = d0 * 100 + d1 * 10 + d2;
            return true;
        }

        private static string NormalizeInput(string input) => input.Trim().ToUpperInvariant();
    }
}
