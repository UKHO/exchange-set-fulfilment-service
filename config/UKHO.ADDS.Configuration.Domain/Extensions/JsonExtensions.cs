using System.Text;

namespace UKHO.ADDS.Configuration.Extensions
{
    public static class JsonExtensions
    {
        /// <summary>
        ///     Strips JavaScript-style // line comments and /* ... */ block comments from a JSON string,
        ///     while preserving content inside string literals.
        /// </summary>
        public static string StripJsonComments(this string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            var sb = new StringBuilder();
            var inString = false;
            var inSingleLineComment = false;
            var inMultiLineComment = false;

            for (var i = 0; i < json.Length; i++)
            {
                var c = json[i];
                var next = i + 1 < json.Length ? json[i + 1] : '\0';

                if (inSingleLineComment)
                {
                    if (c == '\r' || c == '\n')
                    {
                        inSingleLineComment = false;
                        sb.Append(c);
                    }
                    // Else skip
                }
                else if (inMultiLineComment)
                {
                    if (c == '*' && next == '/')
                    {
                        inMultiLineComment = false;
                        i++; // Skip /
                    }
                    // Else skip
                }
                else if (inString)
                {
                    if (c == '\\')
                    {
                        // Escape sequence
                        sb.Append(c);
                        if (next != '\0')
                        {
                            sb.Append(next);
                            i++;
                        }
                    }
                    else if (c == '"')
                    {
                        inString = false;
                        sb.Append(c);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == '/' && next == '/')
                    {
                        inSingleLineComment = true;
                        i++; // Skip second /
                    }
                    else if (c == '/' && next == '*')
                    {
                        inMultiLineComment = true;
                        i++; // Skip *
                    }
                    else if (c == '"')
                    {
                        inString = true;
                        sb.Append(c);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            return sb.ToString();
        }
    }
}
