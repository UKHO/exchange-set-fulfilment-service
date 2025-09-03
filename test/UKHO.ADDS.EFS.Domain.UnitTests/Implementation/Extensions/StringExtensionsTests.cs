using UKHO.ADDS.EFS.Domain.Implementation.Extensions;
using Xunit;

namespace UKHO.ADDS.EFS.Domain.UnitTests.Implementation.Extensions
{
    public sealed class StringExtensionsTests
    {
        [Fact]
        public void NullInput_ReturnsEmpty()
        {
            string? input = null;

            // ReSharper disable once InvokeAsExtensionMethod
            var result = StringExtensions.RemoveControlCharacters(input!); // call as static with null
            Assert.Equal(string.Empty, result);

            // Also verify extension-call style on a null reference:
            string? input2 = null;
            var result2 = input2!.RemoveControlCharacters();

            Assert.Equal(string.Empty, result2);
        }

        [Fact]
        public void EmptyInput_ReturnsEmpty()
        {
            var result = "".RemoveControlCharacters();

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void NoControls_ReturnsOriginalContent()
        {
            const string input = "Hello World 123 _-!αβγ";
            var result = input.RemoveControlCharacters();

            Assert.Equal(input, result);
        }

        [Fact]
        public void Removes_AsciiControls_CR_LF_TAB_VTAB()
        {
            var input = "A\rB\nC\tD\vE";
            var result = input.RemoveControlCharacters();

            Assert.Equal("ABCDE", result);
        }

        [Fact]
        public void Removes_Del_0x7F()
        {
            var input = $"A{(char)0x7F}B";
            var result = input.RemoveControlCharacters();

            Assert.Equal("AB", result);
        }

        [Fact]
        public void Removes_C1_Controls_0x80_to_0x9F()
        {
            var c1 = new string(new[] { (char)0x80, (char)0x85, (char)0x9F });
            var input = $"X{c1}Y";
            var result = input.RemoveControlCharacters();

            Assert.Equal("XY", result);
        }

        [Fact]
        public void Mixed_Text_And_Controls_AllControlsRemoved_TextPreserved()
        {
            var input = "A\nB" + (char)0x85 + "C" + (char)0x7F + "D\tE";
            var result = input.RemoveControlCharacters();

            Assert.Equal("ABCDE", result);
        }

        [Fact]
        public void Preserves_Unicode_Format_And_Separators_Eg_ZWJ_NBSP_LineSeparator()
        {
            // Zero Width Joiner U+200D (Format, Cf) -> should be preserved (char.IsControl == false; custom filter doesn't remove it)
            var zwj = '\u200D';

            // NBSP U+00A0 (Space_Separator, Zs) -> preserved
            var nbsp = '\u00A0';

            // Line Separator U+2028 (Zl) -> preserved
            var lineSep = '\u2028';

            var input = $"A{zwj}B{nbsp}C{lineSep}D";
            var result = input.RemoveControlCharacters();

            Assert.Equal(input, result);
        }

        [Fact]
        public void Preserves_Emoji_And_SurrogatePairs()
        {
            // 😀 U+1F600 (surrogate pair)
            const string emoji = "😀";
            var input = $"Hello {emoji} World";
            var result = input.RemoveControlCharacters();

            Assert.Equal(input, result);
        }

        [Fact]
        public void OnlyControls_ReturnsEmpty()
        {
            var onlyControls = new string(new[]
            {
                '\u0000','\u0001','\u0002','\u0003','\u0004','\u0005','\u0006','\u0007',
                '\u0008','\u0009','\u000A','\u000B','\u000C','\u000D','\u000E','\u000F',
                '\u0010','\u0011','\u0012','\u0013','\u0014','\u0015','\u0016','\u0017',
                '\u0018','\u0019','\u001A','\u001B','\u001C','\u001D','\u001E','\u001F',
                '\u007F','\u0080','\u0085','\u009F'
            });

            var result = onlyControls.RemoveControlCharacters();
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Trims_ControlCharacters_From_Beginning_And_End()
        {
            var input = "\u0001\u0002Hello\u0003\u0004";
            var result = input.RemoveControlCharacters();

            Assert.Equal("Hello", result);
        }

        [Fact]
        public void LargeInput_MixedControls_CorrectlyStrips_All()
        {
            var text = string.Concat(Enumerable.Repeat("AB", 1000));
            var controls = new string(new[] { '\u0000', '\u0009', '\u000A', '\u007F', '\u0085', '\u009F' });
            var input = string.Join("", Enumerable.Repeat(text + controls, 100));
            var expected = string.Join("", Enumerable.Repeat(text, 100));

            var result = input.RemoveControlCharacters();

            Assert.Equal(expected, result);
        }
    }
}
