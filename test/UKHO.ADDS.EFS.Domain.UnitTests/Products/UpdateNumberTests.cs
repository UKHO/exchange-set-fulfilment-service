using System.ComponentModel.DataAnnotations;
using UKHO.ADDS.EFS.Domain.Products;
using Xunit;

namespace UKHO.ADDS.EFS.Domain.UnitTests.Products
{
    public sealed class UpdateNumberTests
    {
        [Fact]
        public void From_Zero_Succeeds_And_Equals_NotSet()
        {
            var v = UpdateNumber.From(0);

            Assert.Equal(0, v.Value);
            Assert.Equal(0, UpdateNumber.NotSet.Value);
            Assert.Equal(UpdateNumber.NotSet, v);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(42)]
        [InlineData(int.MaxValue)]
        public void From_Positive_Succeeds(int val)
        {
            var v = UpdateNumber.From(val);
            Assert.Equal(val, v.Value);
        }

        [Fact]
        public void From_Negative_ThrowsValidationException_WithHelpfulMessage()
        {
            var ex = Assert.Throws<ValidationException>(() => UpdateNumber.From(-1));
            Assert.Contains("UpdateNumber must be >= 0", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void From_IntMinValue_ThrowsValidationException()
        {
            var ex = Assert.Throws<ValidationException>(() => UpdateNumber.From(int.MinValue));
            Assert.Contains("UpdateNumber must be >= 0", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void TryFrom_Zero_ReturnsTrue_AndOutputsZero_Equals_NotSet()
        {
            var ok = UpdateNumber.TryFrom(0, out var v);
            Assert.True(ok);
            Assert.Equal(0, v.Value);
            Assert.Equal(UpdateNumber.NotSet, v);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(999)]
        public void TryFrom_Positive_ReturnsTrue_AndOutputsValue(int val)
        {
            var ok = UpdateNumber.TryFrom(val, out var v);
            Assert.True(ok);
            Assert.Equal(val, v.Value);
        }

        [Fact]
        public void TryFrom_Invalid_DoesNotThrow()
        {
            var ex = Record.Exception(() => UpdateNumber.TryFrom(-1, out _));
            Assert.Null(ex);
        }

        [Fact]
        public void NotSet_Equals_FromZero_And_HasStableHashCode()
        {
            var notSet = UpdateNumber.NotSet;
            var z = UpdateNumber.From(0);

            Assert.Equal(notSet, z);
            Assert.Equal(notSet.GetHashCode(), z.GetHashCode());
        }
    }
}
