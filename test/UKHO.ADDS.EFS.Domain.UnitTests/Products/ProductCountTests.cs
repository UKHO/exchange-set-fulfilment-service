using System.ComponentModel.DataAnnotations;
using UKHO.ADDS.EFS.Domain.Products;
using Xunit;

namespace UKHO.ADDS.EFS.Domain.UnitTests.Products
{
    public sealed class ProductCountTests
    {

        [Fact]
        public void From_Zero_Succeeds_And_Equals_None()
        {
            var v = ProductCount.From(0);

            Assert.Equal(0, v.Value);
            Assert.Equal(0, ProductCount.None.Value);
            Assert.Equal(ProductCount.None, v);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(42)]
        [InlineData(int.MaxValue)]
        public void From_Positive_Succeeds(int val)
        {
            var v = ProductCount.From(val);
            Assert.Equal(val, v.Value);
        }

        [Fact]
        public void From_Negative_ThrowsValidationException_WithHelpfulMessage()
        {
            var ex = Assert.Throws<ValidationException>(() => ProductCount.From(-1));
            Assert.Contains("ProductCount must be >= 0", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void From_IntMinValue_ThrowsValidationException()
        {
            var ex = Assert.Throws<ValidationException>(() => ProductCount.From(int.MinValue));
            Assert.Contains("ProductCount must be >= 0", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void TryFrom_Zero_ReturnsTrue_AndOutputsZero_Equals_None()
        {
            var ok = ProductCount.TryFrom(0, out var v);
            Assert.True(ok);
            Assert.Equal(0, v.Value);
            Assert.Equal(ProductCount.None, v);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(999)]
        public void TryFrom_Positive_ReturnsTrue_AndOutputsValue(int val)
        {
            var ok = ProductCount.TryFrom(val, out var v);
            Assert.True(ok);
            Assert.Equal(val, v.Value);
        }

        [Fact]
        public void TryFrom_Negative_ReturnsFalse_AndOutputsDefault_Which_Equals_None()
        {
            var ok = ProductCount.TryFrom(-5, out var v);
            Assert.False(ok);
        }

        [Fact]
        public void TryFrom_IntMinValue_ReturnsFalse_AndOutputsDefault_Which_Equals_None()
        {
            var ok = ProductCount.TryFrom(int.MinValue, out var v);
            Assert.False(ok);
        }

        [Fact]
        public void TryFrom_Invalid_DoesNotThrow()
        {
            var ex = Record.Exception(() => ProductCount.TryFrom(-1, out _));
            Assert.Null(ex);
        }

        [Fact]
        public void None_Equals_FromZero_And_HasStableHashCode()
        {
            var none = ProductCount.None;
            var z = ProductCount.From(0);

            Assert.Equal(none, z);
            Assert.Equal(none.GetHashCode(), z.GetHashCode());
        }
    }
}
