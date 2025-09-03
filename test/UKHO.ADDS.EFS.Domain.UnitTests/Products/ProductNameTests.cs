using UKHO.ADDS.EFS.Domain.Exceptions;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using Xunit;

namespace UKHO.ADDS.EFS.Domain.UnitTests.Products
{
    public sealed class ProductNameTests
    {
        public static IEnumerable<object[]> S100Codes() => new[]
            {
                101,102,104,111,121,122,124,125,126,127,128,129,130,131,164
            }.Select(c => new object[] { c });

        [Fact]
        public void From_Null_ThrowsValidationException()
        {
            string? input = null;
            var ex = Assert.Throws<ValidationException>(() => ProductName.From(input!));
            Assert.Contains("Cannot create a value object with null", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void From_Empty_ThrowsValidationException()
        {
            var ex = Assert.Throws<ValidationException>(() => ProductName.From(""));
            Assert.Contains("ProductName cannot be null or empty", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void From_WhitespaceOnly_AfterNormalization_Invalid()
        {
            var ex = Assert.Throws<ValidationException>(() => ProductName.From(" \t \r\n "));
            Assert.Contains("ProductName cannot be null or empty", ex.Message, StringComparison.Ordinal);
        }

        [Theory]
        [MemberData(nameof(S100Codes))]
        public void From_S100_Code_WithSuffix_Normalizes_And_Maps(int code)
        {
            // Mixed case + padding; suffix is arbitrary and must be uppercased
            var raw = $"  {code}ab-C  ";
            var pn = ProductName.From(raw);

            Assert.Equal($"{code}AB-C", pn.Value);                  // Trim + ToUpperInvariant
            Assert.Equal(DataStandard.S100, pn.DataStandard);       // S-100 based on 3-digit prefix
            Assert.Equal((DataStandardProductType)code, pn.DataStandardProduct.AsEnum);
        }

        [Theory]
        [MemberData(nameof(S100Codes))]
        public void From_S100_Code_MinimalThreeDigits_Valid(int code)
        {
            var pn = ProductName.From(code.ToString());
            Assert.Equal(code.ToString(), pn.Value);                // exactly the three digits, already uppercase
            Assert.Equal(DataStandard.S100, pn.DataStandard);
            Assert.Equal((DataStandardProductType)code, pn.DataStandardProduct.AsEnum);
        }

        [Theory]
        [InlineData("ABCDEFGH")]
        [InlineData("ab12cd34")]
        [InlineData("12a4abcd")]
        public void From_S57_Length8_Normalizes_And_Maps(string raw)
        {
            var padded = $"  {raw}  ";
            var pn = ProductName.From(padded);

            Assert.Equal(raw.ToUpperInvariant(), pn.Value); // Trim + ToUpperInvariant
            Assert.Equal(DataStandard.S57, pn.DataStandard);
            Assert.Equal(DataStandardProductType.S57, pn.DataStandardProduct.AsEnum);
        }

        [Fact]
        public void From_ThreeZeros_000_IsInvalid_NotAValid_S100_Product()
        {
            var ex = Assert.Throws<ValidationException>(() => ProductName.From("000xyz"));
            Assert.Contains("'000'", ex.Message, StringComparison.Ordinal);
            Assert.Contains("not a valid S-100 product", ex.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("103abc")]   // 103 is not in your enum (invalid S-100 code)
        [InlineData("999whatever")]
        public void From_Unknown_S100_Code_IsInvalid(string input)
        {
            var ex = Assert.Throws<ValidationException>(() => ProductName.From(input));
            Assert.Contains("not a valid S-100 product", ex.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("12")]       // too short for 3-digit check; not length 8 either
        [InlineData("1A2")]      // not 3 digits
        [InlineData("ABCDEF")]   // length 6, not 8
        [InlineData("10XREST")]  // fails 3-digit check; not length 8
        public void From_InvalidShape_Throws(string input)
        {
            var ex = Assert.Throws<ValidationException>(() => ProductName.From(input));
            Assert.Contains("neither starts with a 3-digit S-100 code nor has length 8 for S-57",
                ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void DataStandardProperty_S100_Example()
        {
            var pn = ProductName.From("121hello");
            Assert.Equal(DataStandard.S100, pn.DataStandard);
            Assert.Equal(DataStandardProductType.S121, pn.DataStandardProduct.AsEnum);
        }

        [Fact]
        public void DataStandardProperty_S57_Example()
        {
            var pn = ProductName.From("ab12cd34");
            Assert.Equal(DataStandard.S57, pn.DataStandard);
            Assert.Equal(DataStandardProductType.S57, pn.DataStandardProduct.AsEnum);
        }
    }
}
