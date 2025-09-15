using System.Diagnostics.CodeAnalysis;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.scs.Constants
{
    [ExcludeFromCodeCoverage]
    public class ErrorResponseConstants
    {
        /// <summary>
        /// RFC 9110 Section 15.5.16 - Unsupported Media Type
        /// </summary>
        public const string UnsupportedMediaTypeUri = "https://tools.ietf.org/html/rfc9110#section-15.5.16";

        /// <summary>
        /// Default fallback URI for generic error responses
        /// </summary>
        public const string GenericErrorUri = "https://example.com";
    }
}
