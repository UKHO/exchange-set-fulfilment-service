using System.ComponentModel.DataAnnotations;
using UKHO.ADDS.EFS.Domain.Files;
using Xunit;

namespace UKHO.ADDS.EFS.Domain.UnitTests.Files
{
    public sealed class FileSizeTests
    {
        [Fact]
        public void WhenFromZero_ThenEqualsZero()
        {
            var v = FileSize.From(0);

            Assert.Equal(0, v.Value);
            Assert.Equal(0, FileSize.Zero.Value);
            Assert.Equal(FileSize.Zero, v);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(42)]
        [InlineData(1024)]
        [InlineData(10485760)] // 10MB
        [InlineData(long.MaxValue)]
        public void WhenFromPositive_ThenEqualsValue(long val)
        {
            var v = FileSize.From(val);
            Assert.Equal(val, v.Value);
        }

        [Fact]
        public void WhenFromNegative_ThenThrowsValidationException_WithHelpfulMessage()
        {
            var ex = Assert.Throws<ValidationException>(() => FileSize.From(-1));
            Assert.Contains("FileSize must be >= 0", ex.Message, System.StringComparison.Ordinal);
        }

        [Fact]
        public void WhenFromLongMinValue_ThenThrowsValidationException()
        {
            var ex = Assert.Throws<ValidationException>(() => FileSize.From(long.MinValue));
            Assert.Contains("FileSize must be >= 0", ex.Message, System.StringComparison.Ordinal);
        }

        [Fact]
        public void WhenTryFromZero_ThenReturnsTrue_AndEqualsZero()
        {
            var ok = FileSize.TryFrom(0, out var v);
            Assert.True(ok);
            Assert.Equal(0, v.Value);
            Assert.Equal(FileSize.Zero, v);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(999)]
        [InlineData(1048576)] // 1MB
        public void WhenTryFromPositive_ThenReturnsTrue_AndEqualsValue(long val)
        {
            var ok = FileSize.TryFrom(val, out var v);
            Assert.True(ok);
            Assert.Equal(val, v.Value);
        }

        [Fact]
        public void WhenTryFromInvalid_ThenDoesNotThrow()
        {
            var ex = Record.Exception(() => FileSize.TryFrom(-1, out _));
            Assert.Null(ex);
        }

        [Fact]
        public void WhenTryFromInvalid_ThenReturnsFalse()
        {
            var ok = FileSize.TryFrom(-1, out var v);
            Assert.False(ok);
            // For invalid values, TryFrom should return false and the out parameter will be uninitialized
            // We can't use default(FileSize) so we just verify the method returns false
        }

        [Fact]
        public void WhenZeroAndFromZero_ThenEqualsAndHasStableHashCode()
        {
            var zero = FileSize.Zero;
            var z = FileSize.From(0);

            Assert.Equal(z, zero);
            Assert.Equal(z.GetHashCode(), zero.GetHashCode());
        }
    }
}
