using UKHO.ADDS.EFS.Domain.Exceptions;
using UKHO.ADDS.EFS.Domain.Jobs;
using Xunit;

namespace UKHO.ADDS.EFS.Domain.UnitTests.Jobs
{
    public sealed class BatchIdValidationTests
    {
        [Theory]
        [InlineData("A")]
        [InlineData("batch-001")]
        [InlineData("x y z")]
        [InlineData("0")]
        [InlineData(" \u00A0x")] // leading NBSP + 'x' is still non-whitespace overall
        public void From_Valid_NonEmptyNonWhitespace_Succeeds(string input)
        {
            var id = BatchId.From(input);
            Assert.Equal(input, id.Value);
        }

        [Fact]
        public void From_Empty_ThrowsValidationException_WithHelpfulMessage()
        {
            var ex = Assert.Throws<ValidationException>(() => BatchId.From(string.Empty));
            Assert.Contains("BatchId must not be empty or whitespace", ex.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\r\n")]
        [InlineData(" \t ")]
        public void From_Whitespace_ThrowsValidationException(string input)
        {
            var ex = Assert.Throws<ValidationException>(() => BatchId.From(input));
            Assert.Contains("BatchId must not be empty or whitespace", ex.Message, StringComparison.Ordinal);
        }
        
        [Theory]
        [InlineData("abc")]
        [InlineData("123")]
        public void TryFrom_Valid_ReturnsTrue_AndOutputsValue(string input)
        {
            var ok = BatchId.TryFrom(input, out var id);
            Assert.True(ok);
            Assert.Equal(input, id.Value);
        }

        [Fact]
        public void TryFrom_Empty_ReturnsFalse_AndOutputsDefault_NotEqualToNone()
        {
            var ok = BatchId.TryFrom(string.Empty, out var id);
            Assert.False(ok);

            // The out value is default(BatchId) for failures; it should NOT be equal to the special instance
            Assert.NotEqual(BatchId.None, id);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\r\n")]
        [InlineData(" \t ")]
        public void TryFrom_Whitespace_ReturnsFalse_AndOutputsDefault(string input)
        {
            var ok = BatchId.TryFrom(input, out var id);
            Assert.False(ok);
            Assert.NotEqual(BatchId.None, id);
        }

        [Fact]
        public void TryFrom_Null_ReturnsFalse_AndOutputsDefault()
        {
            string? input = null;
            var ok = BatchId.TryFrom(input!, out var id);
            Assert.False(ok);
            Assert.NotEqual(BatchId.None, id);
        }

        [Fact]
        public void TryFrom_Invalid_DoesNotThrow()
        {
            var ex = Record.Exception(() => BatchId.TryFrom("", out _));
            Assert.Null(ex);
        }

        [Fact]
        public void Instance_None_Is_EmptyString_And_IsNotConstructibleVia_From()
        {
            // The special instance must exist and represent an empty underlying value
            Assert.Equal("", BatchId.None.Value);

            // But attempting to construct the same value via From("") must fail per validation
            Assert.Throws<ValidationException>(() => BatchId.From(""));
        }
    }
}
