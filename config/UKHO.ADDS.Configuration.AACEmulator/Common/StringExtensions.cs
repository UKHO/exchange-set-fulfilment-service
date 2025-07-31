using System.Text;

namespace UKHO.ADDS.Configuration.AACEmulator.Common
{
    public static class StringExtensions
    {
        public static string Unescape(this string s)
        {
            var builder = new StringBuilder();

            for (var i = 0; i < s.Length; i++)
            {
                if (s[i] is '\\' && i < s.Length - 1)
                {
                    i++;
                }

                builder.Append(s[i]);
            }

            return builder.ToString();
        }
    }
}
