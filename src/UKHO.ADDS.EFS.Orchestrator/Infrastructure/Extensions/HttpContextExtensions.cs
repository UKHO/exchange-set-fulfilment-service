using UKHO.ADDS.Clients.Common.Constants;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Extensions
{
    internal static class HttpContextExtensions
    {
        public static string GetCorrelationId(this HttpContext httpContext)
        {
            return httpContext.Request.Headers.TryGetValue(ApiHeaderKeys.XCorrelationIdHeaderKey, out var correlationId) ? correlationId.ToString() : string.Empty;
        }
    }
}
