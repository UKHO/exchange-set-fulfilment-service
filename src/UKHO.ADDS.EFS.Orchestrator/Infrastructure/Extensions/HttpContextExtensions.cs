using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.EFS.VOS;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Extensions
{
    internal static class HttpContextExtensions
    {
        public static CorrelationId GetCorrelationId(this HttpContext httpContext)
        {
            if (httpContext.Request.Headers.TryGetValue(ApiHeaderKeys.XCorrelationIdHeaderKey, out var correlationId))
            {
                return CorrelationId.From(correlationId.ToString());
            }

            return CorrelationId.None;
        }
    }
}
