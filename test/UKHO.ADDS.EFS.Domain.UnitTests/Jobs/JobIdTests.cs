using System.ComponentModel.DataAnnotations;
using UKHO.ADDS.EFS.Domain.Jobs;
using Xunit;

namespace UKHO.ADDS.EFS.Domain.UnitTests.Jobs
{
    public sealed class JobIdTests
    {
        [Fact]
        public void From_Valid_ReturnsValue_AsIs_When_NoTrimNeeded()
        {
            var id = JobId.From("abc");
            Assert.Equal("abc", id.Value);
        }

        [Fact]
        public void From_Valid_LeadingAndTrailingSpaces_AreTrimmed()
        {
            var id = JobId.From("  abc  ");
            Assert.Equal("abc", id.Value);
        }

        [Fact]
        public void From_Valid_TabsAndNewlines_AreTrimmed()
        {
            var id = JobId.From("\t\r\nabc\r\n\t");
            Assert.Equal("abc", id.Value);
        }

        [Fact]
        public void From_Valid_NonBreakingSpace_IsTrimmed()
        {
            var id = JobId.From("\u00A0abc\u00A0");
            Assert.Equal("abc", id.Value);
        }

        [Fact]
        public void From_Valid_InternalWhitespace_IsPreserved()
        {
            var id = JobId.From("ab  c");
            Assert.Equal("ab  c", id.Value);
        }

        [Fact]
        public void From_Empty_ThrowsValidationException_WithHelpfulMessage()
        {
            var ex = Assert.Throws<ValidationException>(() => JobId.From(string.Empty));
            Assert.Contains("JobId cannot be null or empty", ex.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\r\n")]
        [InlineData(" \t \r\n ")]
        [InlineData("\u00A0")] // NBSP-only -> trims to empty -> invalid
        public void From_WhitespaceOnly_ThrowsValidationException(string input)
        {
            var ex = Assert.Throws<ValidationException>(() => JobId.From(input));
            Assert.Contains("JobId cannot be null or empty", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void TryFrom_Valid_ReturnsTrue_AndOutputs_TrimmedValue()
        {
            var ok = JobId.TryFrom("  abc  ", out var id);
            Assert.True(ok);
            Assert.Equal("abc", id.Value);
        }

        [Fact]
        public void TryFrom_Empty_ReturnsFalse_AndOutputsDefault()
        {
            var ok = JobId.TryFrom("", out var id);
            Assert.False(ok);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\r\n")]
        [InlineData(" \t \r\n ")]
        [InlineData("\u00A0")]
        public void TryFrom_WhitespaceOnly_ReturnsFalse_AndOutputsDefault(string input)
        {
            var ok = JobId.TryFrom(input, out var id);
            Assert.False(ok);
        }

        [Fact]
        public void TryFrom_Null_ReturnsFalse_AndOutputsDefault_WithoutThrowing()
        {
            string? input = null;
            var ex = Record.Exception(() => JobId.TryFrom(input!, out _));
            Assert.Null(ex);

            var ok = JobId.TryFrom(input!, out var id);
            Assert.False(ok);
        }
    }
}
