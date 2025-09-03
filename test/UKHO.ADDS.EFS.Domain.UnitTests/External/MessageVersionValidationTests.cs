using UKHO.ADDS.EFS.Domain.Exceptions;
using UKHO.ADDS.EFS.Domain.Messages;
using Xunit;

namespace UKHO.ADDS.EFS.Domain.UnitTests.External
{
    public sealed class MessageVersionValidationTests
    {
        [Fact]
        public void From_MinBoundary_One_Succeeds()
        {
            var mv = MessageVersion.From(1);
            Assert.Equal(1, mv.Value);
        }

        [Fact]
        public void From_PositiveValue_Succeeds()
        {
            var mv = MessageVersion.From(42);

            Assert.Equal(42, mv.Value);
        }

        [Fact]
        public void From_Zero_ThrowsValidationException_WithHelpfulMessage()
        {
            var ex = Assert.Throws<ValidationException>(() => MessageVersion.From(0));

            Assert.Contains("MessageVersion must be >= 1", ex.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-10)]
        [InlineData(int.MinValue)]
        public void From_NegativeValues_ThrowValidationException(int value)
        {
            var ex = Assert.Throws<ValidationException>(() => MessageVersion.From(value));

            Assert.Contains("MessageVersion must be >= 1", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void TryFrom_Valid_ReturnsTrue_AndOutputsValue()
        {
            var ok = MessageVersion.TryFrom(7, out var mv);

            Assert.True(ok);
            Assert.Equal(7, mv.Value);
        }

        [Fact]
        public void TryFrom_Zero_ReturnsFalse_AndOutputsDefault()
        {
            var ok = MessageVersion.TryFrom(0, out var mv);

            Assert.False(ok);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-999)]
        public void TryFrom_Negative_ReturnsFalse_AndOutputsDefault(int value)
        {
            var ok = MessageVersion.TryFrom(value, out var mv);

            Assert.False(ok);
        }

        [Fact]
        public void TryFrom_Invalid_DoesNotThrow()
        {
            var ex = Record.Exception(() => MessageVersion.TryFrom(0, out _));

            Assert.Null(ex);
        }
    }
}
