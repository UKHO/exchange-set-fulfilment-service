using System.Diagnostics.CodeAnalysis;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.scs.Constants
{
    [ExcludeFromCodeCoverage]
    public class TraceConstants
    {
        /// <summary>
        /// Length of trace ID substring when generated from GUID.
        /// Takes first 23 characters of GUID (format: "12345678-1234-1234-1234-123") 
        /// to create a shorter, readable trace ID for mock responses.
        /// </summary>
        public const int MockTraceIdLength = 23;
    }
}
