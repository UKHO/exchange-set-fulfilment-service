using UKHO.ADDS.EFS.Constants;

namespace UKHO.ADDS.EFS.Orchestrator.Middleware
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            if (!httpContext.Request.Headers.TryGetValue(ApiHeaderKeys.XCorrelationIdHeaderKey, out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                httpContext.Request.Headers[ApiHeaderKeys.XCorrelationIdHeaderKey] = correlationId;
                _logger.LogInformation("No correlation ID found. Generated new one: {CorrelationId}", correlationId!);
            }
            else
            {
                _logger.LogInformation("Using existing correlation ID: {CorrelationId}", correlationId!);
            }

            httpContext.Response.Headers[ApiHeaderKeys.XCorrelationIdHeaderKey] = correlationId;

            var state = new Dictionary<string, object>
            {
                [ApiHeaderKeys.XCorrelationIdHeaderKey] = correlationId!,
            };

            using (_logger.BeginScope(state))
            {
                await _next(httpContext);
            }
        }
    }
}
