using System.ComponentModel.DataAnnotations;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using Xunit;
using DataStandardProduct = UKHO.ADDS.EFS.Domain.Products.DataStandardProduct;

namespace UKHO.ADDS.EFS.Domain.UnitTests.Products
{
    public sealed class DataStandardProductTests
    {
        public static IEnumerable<object[]> AllEnumValues() => Enum.GetValues(typeof(DataStandardProductType)).Cast<DataStandardProductType>().Select(x => new object[]
            {
                x
            });

        public static IEnumerable<object[]> AllEnumNamesAndValues()
        {
            foreach (var name in Enum.GetNames(typeof(DataStandardProductType)))
            {
                var value = (DataStandardProductType)Enum.Parse(typeof(DataStandardProductType), name);
                yield return
                [
                    name, value
                ];
            }
        }

        private static DataStandard ExpectedStandard(DataStandardProductType t) => t == DataStandardProductType.S57 ? DataStandard.S57 : DataStandard.S100;

        [Theory]
        [MemberData(nameof(AllEnumValues))]
        public void FromEnum_Yields_Matching_AsEnum_Value_And_DataStandard(DataStandardProductType t)
        {
            var dsp = DataStandardProduct.FromEnum(t);

            Assert.Equal(t, dsp.AsEnum);
            Assert.Equal((int)t, dsp.Value);
            Assert.Equal(ExpectedStandard(t), dsp.DataStandard);

            // Also check static properties for known values
            var viaStatic = t switch
            {
                DataStandardProductType.S57 => DataStandardProduct.S57,
                DataStandardProductType.S101 => DataStandardProduct.S101,
                DataStandardProductType.S102 => DataStandardProduct.S102,
                DataStandardProductType.S104 => DataStandardProduct.S104,
                DataStandardProductType.S111 => DataStandardProduct.S111,
                DataStandardProductType.S121 => DataStandardProduct.S121,
                DataStandardProductType.S122 => DataStandardProduct.S122,
                DataStandardProductType.S124 => DataStandardProduct.S124,
                DataStandardProductType.S125 => DataStandardProduct.S125,
                DataStandardProductType.S126 => DataStandardProduct.S126,
                DataStandardProductType.S127 => DataStandardProduct.S127,
                DataStandardProductType.S128 => DataStandardProduct.S128,
                DataStandardProductType.S129 => DataStandardProduct.S129,
                DataStandardProductType.S130 => DataStandardProduct.S130,
                DataStandardProductType.S131 => DataStandardProduct.S131,
                DataStandardProductType.S164 => DataStandardProduct.S164,
                var _ => DataStandardProduct.FromEnum(t) // fallback (shouldn't hit with current list)
            };

            Assert.Equal(dsp, viaStatic);
        }

        [Fact]
        public void From_UnknownCode_ThrowsValidationException()
        {
            const int unknown = 99999;
            var ex = Assert.Throws<ValidationException>(() => DataStandardProduct.From(unknown));

            Assert.Contains("Unknown DataStandardProduct code", ex.Message, StringComparison.Ordinal);
            Assert.Contains(unknown.ToString(), ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void TryFrom_UnknownCode_ReturnsFalse_AndOutputsDefault()
        {
            var ok = DataStandardProduct.TryFrom(123456, out var dsp);

            Assert.False(ok);
        }

        [Fact]
        public void S57_Has_DataStandard_S57()
        {
            var dsp = DataStandardProduct.S57;

            Assert.Equal(DataStandardProductType.S57, dsp.AsEnum);
            Assert.Equal(DataStandard.S57, dsp.DataStandard);
        }

        [Theory]
        [InlineData(DataStandardProductType.S101)]
        [InlineData(DataStandardProductType.S102)]
        [InlineData(DataStandardProductType.S104)]
        [InlineData(DataStandardProductType.S111)]
        [InlineData(DataStandardProductType.S121)]
        [InlineData(DataStandardProductType.S122)]
        [InlineData(DataStandardProductType.S124)]
        [InlineData(DataStandardProductType.S125)]
        [InlineData(DataStandardProductType.S126)]
        [InlineData(DataStandardProductType.S127)]
        [InlineData(DataStandardProductType.S128)]
        [InlineData(DataStandardProductType.S129)]
        [InlineData(DataStandardProductType.S130)]
        [InlineData(DataStandardProductType.S131)]
        [InlineData(DataStandardProductType.S164)]
        public void All_S100_Codes_Have_DataStandard_S100(DataStandardProductType t)
        {
            var dsp = DataStandardProduct.FromEnum(t);

            Assert.Equal(DataStandard.S100, dsp.DataStandard);
        }

        [Theory]
        [MemberData(nameof(AllEnumNamesAndValues))]
        public void From_String_ExactEnumName_Parses_To_Same_Value(string name, DataStandardProductType expected)
        {
            var dsp = DataStandardProduct.From(name);

            Assert.Equal(expected, dsp.AsEnum);
            Assert.Equal((int)expected, dsp.Value);
            Assert.Equal(ExpectedStandard(expected), dsp.DataStandard);
        }

        [Theory]
        [InlineData("s57", DataStandardProductType.S57)]
        [InlineData("S101", DataStandardProductType.S101)]
        [InlineData("s102", DataStandardProductType.S102)]
        [InlineData("S131", DataStandardProductType.S131)]
        [InlineData("s164", DataStandardProductType.S164)]
        public void From_String_Is_Case_Insensitive(string input, DataStandardProductType expected)
        {
            var dsp = DataStandardProduct.From(input);

            Assert.Equal(expected, dsp.AsEnum);
        }

        [Theory]
        [InlineData("  S57  ", DataStandardProductType.S57)]
        [InlineData("\tS101\n", DataStandardProductType.S101)]
        [InlineData("\r\n S102\t", DataStandardProductType.S102)]
        public void From_String_Trims_Whitespace(string input, DataStandardProductType expected)
        {
            var dsp = DataStandardProduct.From(input);

            Assert.Equal(expected, dsp.AsEnum);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\r\n")]
        public void From_String_EmptyOrWhitespace_Throws_ValidationException(string? input)
        {
            var ex = Assert.Throws<ValidationException>(() => DataStandardProduct.From(input!));

            Assert.Contains("cannot be created from an empty string", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("S999")]
        [InlineData("S103")] // not a defined product
        [InlineData("Nope")]
        [InlineData("S-101")]
        public void From_String_UnknownName_Throws_ValidationException(string input)
        {
            var ex = Assert.Throws<ValidationException>(() => DataStandardProduct.From(input));

            Assert.Contains("Unknown DataStandardProduct name", ex.Message, StringComparison.Ordinal);
            Assert.Contains(input, ex.Message, StringComparison.Ordinal);
        }
    }
}
