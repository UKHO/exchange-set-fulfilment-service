using System.Diagnostics.CodeAnalysis;

namespace UKHO.ADDS.EFS.Constants
{
    [ExcludeFromCodeCoverage]
    public static class ApiHeaderKeys
    {
        public const string XCorrelationIdHeaderKey = "X-Correlation-ID";

        public const string OriginHeaderKey = "origin";

        public const string ContentType = "application/json";
    }
}
