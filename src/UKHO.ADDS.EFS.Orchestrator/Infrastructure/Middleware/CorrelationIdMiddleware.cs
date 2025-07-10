using UKHO.ADDS.EFS.Constants;
using UKHO.ADDS.EFS.Exceptions;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Middleware
{
    internal class CorrelationIdMiddleware
    {
        private static readonly string[] _pathsWithoutCorrelationId = ["/scalar", "/healthcheck", "/openapi", "/jobs", "/favicon.ico"];

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext httpContext)
        {
            // TODO This is really fragile, need a better approach to determine if a correlation ID is required.

            var demandsCorrelationId = _pathsWithoutCorrelationId.All(v => httpContext.Request.Path.Value?.Contains(v, StringComparison.OrdinalIgnoreCase) != true);

            if (demandsCorrelationId || httpContext.Request.Method == "POST")
            {
                var hasCorrelationId = httpContext.Request.Headers.TryGetValue(ApiHeaderKeys.XCorrelationIdHeaderKey, out var correlationId);

                if (!hasCorrelationId)
                {
                    throw new OrchestratorException("No correlation ID found in the request header");
                }
#if DEBUG
                // Make the correlation ID unique for testing/debugging purposes.
                var debugCorrelationId = $"{correlationId}-{DateTime.UtcNow:yyMMddHHmmss}";

                httpContext.Request.Headers[ApiHeaderKeys.XCorrelationIdHeaderKey] = debugCorrelationId;
                httpContext.Response.Headers[ApiHeaderKeys.XCorrelationIdHeaderKey] = debugCorrelationId;
#else
                httpContext.Response.Headers[ApiHeaderKeys.XCorrelationIdHeaderKey] = correlationId;
#endif
            }

            await _next(httpContext);
        }
    }
}
