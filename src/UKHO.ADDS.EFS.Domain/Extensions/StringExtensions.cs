using System.Text;

namespace UKHO.ADDS.EFS.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        ///     Removes all control characters from a string, including ASCII and Unicode control codes.
        /// </summary>
        /// <param name="input">The original string.</param>
        /// <returns>A sanitized string with control characters removed.</returns>
        public static string RemoveControlCharacters(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(input.Length);

            foreach (var c in input.Where(c => !char.IsControl(c) && !IsUnicodeControl(c)))
            {
                builder.Append(c);
            }

            return builder.ToString();
        }

        /// <summary>
        ///     Detects additional Unicode control characters not flagged by char.IsControl.
        /// </summary>
        private static bool IsUnicodeControl(char c) =>
            // Unicode control characters include:
            // U+0000 to U+001F  - standard ASCII control characters
            // U+007F            - DEL (also control)
            // U+0080 to U+009F  - C1 control characters (extended)
            // Others may be classified as Format, Separator, etc.
            c is >= '\u0000' and <= '\u001F' or '\u007F' or >= '\u0080' and <= '\u009F';
    }
}
