using UKHO.ADDS.EFS.Constants;

namespace UKHO.ADDS.EFS.Orchestrator.Middleware
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var logger = httpContext.RequestServices.GetRequiredService<ILogger<CorrelationIdMiddleware>>();

            if (!httpContext.Request.Headers.TryGetValue(ApiHeaderKeys.XCorrelationIdHeaderKey, out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                httpContext.Request.Headers[ApiHeaderKeys.XCorrelationIdHeaderKey] = correlationId;
                logger.LogInformation("No correlation ID found. Generated new one: {_X-Correlation-ID}", correlationId!);
            }
            else
            {
                logger.LogInformation("Using existing correlation ID: {_X-Correlation-ID}", correlationId!);
            }

            httpContext.Response.Headers[ApiHeaderKeys.XCorrelationIdHeaderKey] = correlationId;

            await _next(httpContext);
        }
    }
}
