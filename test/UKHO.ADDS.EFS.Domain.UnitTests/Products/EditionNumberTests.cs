using System.ComponentModel.DataAnnotations;
using UKHO.ADDS.EFS.Domain.Products;
using Xunit;

namespace UKHO.ADDS.EFS.Domain.UnitTests.Products
{
    public sealed class EditionNumberTests
    {
        [Fact]
        public void From_Zero_ThrowsValidationException()
        {
            var ex = Assert.Throws<ValidationException>(() => EditionNumber.From(0));
            Assert.Contains("must be a positive integer.", ex.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(42)]
        [InlineData(int.MaxValue)]
        public void From_Positive_Succeeds(int val)
        {
            var v = EditionNumber.From(val);
            Assert.Equal(val, v.Value);
        }

        [Fact]
        public void From_Negative_ThrowsValidationException_WithHelpfulMessage()
        {
            var ex = Assert.Throws<ValidationException>(() => EditionNumber.From(-1));
            Assert.Contains("must be a positive integer.", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void From_IntMinValue_ThrowsValidationException()
        {
            var ex = Assert.Throws<ValidationException>(() => EditionNumber.From(int.MinValue));
            Assert.Contains("must be a positive integer.", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void TryFrom_Zero_ReturnsFalse_AndOutputsDefault()
        {
            var ok = EditionNumber.TryFrom(0, out var v);
            Assert.False(ok);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(999)]
        public void TryFrom_Positive_ReturnsTrue_AndOutputsValue(int val)
        {
            var ok = EditionNumber.TryFrom(val, out var v);
            Assert.True(ok);
            Assert.Equal(val, v.Value);
        }

        [Fact]
        public void Instances_AreEqual_And_ShareSameHash_And_Equal_FromOne()
        {
            var a = EditionNumber.NotRequired;
            var b = EditionNumber.NotSet;
            var z = EditionNumber.From(1);

            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.Equal(z, a);
            Assert.Equal(z, b);
        }
    }
}
