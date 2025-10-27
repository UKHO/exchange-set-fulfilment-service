using System.Diagnostics.CodeAnalysis;

namespace UKHO.ADDS.EFS.Domain.Constants
{
    [ExcludeFromCodeCoverage]
    public static class ApiHeaderKeys
    {
        public const string XCorrelationIdHeaderKey = "x-correlation-id";
        public const string OriginHeaderKey = "x-origin";
        public const string ContentType = "application/json; charset=utf-8";
        public const string ContentTypeOctetStream = "application/octet-stream";
        public const string ContentTypeTextPlain = "text/plain";
        public const string LastModifiedHeaderKey = "Last-Modified";
        public const string IfModifiedSinceHeaderKey = "If-Modified-Since";
        public const string ErrorOriginHeaderKey = "X-Error-Origin-Service";
        public const string ErrorOriginStatusHeaderKey = "X-Error-Origin-Status";
    }
}
