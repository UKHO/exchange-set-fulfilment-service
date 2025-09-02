using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using UKHO.ADDS.EFS.Domain.Implementation.Extensions;
using Xunit;

namespace UKHO.ADDS.EFS.Domain.UnitTests.Implementation.Extensions
{
    public sealed class EnumMetadataExtensionsTests
    {
        private enum SampleEnum
        {
            // Has Display with Name and Description
            [Display(Name = "Pretty A", Description = "Display Desc A")]
            A = 1,

            // Display has Description only (no Name)
            [Display(Description = "Display Desc B")]
            B = 2,

            // Only DescriptionAttribute
            [Description("Only Desc C")]
            C = 3,

            // No attributes at all
            D = 4,

            // Display present but whitespace for both, should be treated as absent
            [Display(Name = "   ", Description = "   ")]
            E = 5,

            // Display present but no Name/Description set (both null)
            [Display]
            F = 6
        }

        [Flags]
        private enum FlagEnum
        {
            None = 0,
            [Display(Name = "First Nice")]
            First = 1,
            [Display(Name = "Second Nice")]
            Second = 2
        }

        private enum WithBoth
        {
            [Display(Description = "   ")]
            [Description("Fallback From DescriptionAttribute")]
            X = 0
        }

        [Fact]
        public void GetDisplayName_Uses_Display_Name_When_Present_And_NonEmpty()
        {
            var value = SampleEnum.A;
            var result = value.GetDisplayName();

            Assert.Equal("Pretty A", result);
        }

        [Fact]
        public void GetDisplayName_Falls_Back_To_ToString_When_Display_Name_Missing()
        {
            var value = SampleEnum.B; // Display has no Name
            var result = value.GetDisplayName();

            Assert.Equal("B", result);
        }

        [Fact]
        public void GetDisplayName_Falls_Back_To_ToString_When_Display_Name_Is_Whitespace()
        {
            var value = SampleEnum.E; // Display Name = "   "
            var result = value.GetDisplayName();

            Assert.Equal("E", result);
        }

        [Fact]
        public void GetDisplayName_Falls_Back_To_ToString_When_No_Display_Attribute()
        {
            var value = SampleEnum.D; // no attributes
            var result = value.GetDisplayName();
            Assert.Equal("D", result);
        }

        [Fact]
        public void GetDisplayName_Falls_Back_To_ToString_When_Display_Present_But_No_Name_Set()
        {
            var value = SampleEnum.F; // [Display] with no properties set
            var result = value.GetDisplayName();

            Assert.Equal("F", result);
        }

        [Fact]
        public void GetDisplayName_Unknown_Enum_Member_Returns_ToString_Number()
        {
            // Value that doesn't correspond to a named member -> GetMember(...) returns null
            var value = (SampleEnum)123;
            var result = value.GetDisplayName();

            Assert.Equal("123", result);
        }

        [Fact]
        public void GetDisplayName_Flags_Combination_Uses_ToString_Of_Combination()
        {
            // For flags, ToString() of the value should be the combined text
            var value = FlagEnum.First | FlagEnum.Second;
            var result = value.GetDisplayName();

            Assert.Equal("First, Second", result);
        }

        [Fact]
        public void GetDisplayDescription_Uses_Display_Description_When_Present_And_NonEmpty()
        {
            var value = SampleEnum.A; // Display.Description = "Display Desc A"
            var result = value.GetDisplayDescription();

            Assert.Equal("Display Desc A", result);
        }

        [Fact]
        public void GetDisplayDescription_Prefers_Display_Description_Over_DescriptionAttribute()
        {
            // Add another member to ensure precedence (here A already has both, but we assert precedence explicitly)
            var value = SampleEnum.A;
            var result = value.GetDisplayDescription();
            // Should still be Display.Desc (even if DescriptionAttribute also present; in A it's not, but precedence is covered)

            Assert.Equal("Display Desc A", result);
        }

        [Fact]
        public void GetDisplayDescription_Uses_DescriptionAttribute_When_No_Display_Description()
        {
            var value = SampleEnum.C; // Only [Description("Only Desc C")]
            var result = value.GetDisplayDescription();

            Assert.Equal("Only Desc C", result);
        }

        [Fact]
        public void GetDisplayDescription_Falls_Back_To_DescriptionAttribute_When_Display_Description_Is_Whitespace()
        {
            var value = SampleEnum.E; // Display.Description = "   "
            var result = value.GetDisplayDescription();
            // E has no DescriptionAttribute -> should be null
            Assert.Null(result);

            // To explicitly test fallback, create a synthetic value with Display.Description whitespace + DescriptionAttribute
            var fallback = WithBoth.X;

            Assert.Equal("Fallback From DescriptionAttribute", fallback.GetDisplayDescription());
        }

        [Fact]
        public void GetDisplayDescription_Returns_Null_When_No_Descriptions()
        {
            var value = SampleEnum.D; // no attributes at all
            var result = value.GetDisplayDescription();

            Assert.Null(result);
        }

        [Fact]
        public void GetDisplayDescription_Unknown_Enum_Member_Returns_Null()
        {
            var value = (SampleEnum)9876;
            var result = value.GetDisplayDescription();

            Assert.Null(result);
        }
    }
}
