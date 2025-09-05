using Serilog.Context;
using UKHO.ADDS.EFS.Domain.Constants;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Middleware
{
    internal class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var hasCorrelationId = httpContext.Request.Headers.TryGetValue(ApiHeaderKeys.XCorrelationIdHeaderKey, out var correlationId);

            if (!hasCorrelationId)
            {
                if (httpContext.Request.Path.Equals("/job"))
                {
                    correlationId = $"job-{Guid.NewGuid():N}";

                    httpContext.Request.Headers[ApiHeaderKeys.XCorrelationIdHeaderKey] = correlationId;
                    httpContext.Response.Headers[ApiHeaderKeys.XCorrelationIdHeaderKey] = correlationId;
                }
            }
            else
            {
                httpContext.Response.Headers[ApiHeaderKeys.XCorrelationIdHeaderKey] = correlationId;
            }

            // Push correlation ID to Serilog LogContext for automatic inclusion in all logs
            using (LogContext.PushProperty("CorrelationId", correlationId.ToString()))
            {
                await _next(httpContext);
            }
        }
    }
}
