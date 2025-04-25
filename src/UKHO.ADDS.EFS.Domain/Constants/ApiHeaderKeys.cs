using System.Diagnostics.CodeAnalysis;

namespace UKHO.ADDS.EFS.Constants
{
    [ExcludeFromCodeCoverage]
    public static class ApiHeaderKeys
    {
        public const string XCorrelationIdHeaderKey = "x-correlation-id";
        public const string OriginHeaderKey = "x-origin";

        public const string ContentType = "application/json";
    }
}
