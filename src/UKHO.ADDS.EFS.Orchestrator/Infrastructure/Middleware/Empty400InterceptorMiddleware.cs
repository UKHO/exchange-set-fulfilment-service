using UKHO.ADDS.EFS.Orchestrator.Api.Messages;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Extensions;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Middleware
{
    public class Empty400InterceptorMiddleware
    {
        private readonly RequestDelegate _next;
        public Empty400InterceptorMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            // Capture the original response body stream
            var originalBodyStream = context.Response.Body;
            using (var memoryStream = new MemoryStream())
            {
                // Replace the response body with a memory stream
                context.Response.Body = memoryStream;
                await _next(context);
                // Check if the status code is 400 and the body is empty
                if (context.Response.StatusCode == StatusCodes.Status400BadRequest && memoryStream.Length == 0)
                {
                    // Create a custom error response
                    var errorResponse = new ErrorResponseModel
                    {
                        CorrelationId = context.GetCorrelationId().ToString(),
                        Errors = new List<ErrorDetail>
                            {
                                new()
                                {
                                    Source = "requestBody",
                                    Description = "Either body is null or malformed."
                                }
                            }
                    };
                    var errorJson = System.Text.Json.JsonSerializer.Serialize(errorResponse);
                    var errorBytes = System.Text.Encoding.UTF8.GetBytes(errorJson);
                    // Set the content type and length
                    context.Response.ContentType = "application/json";
                    context.Response.ContentLength = errorBytes.Length;
                    // Write the custom error response to the original body stream
                    await originalBodyStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                }
                else
                {
                    // If not a 400 with empty body, copy the memory stream to the original body stream
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    await memoryStream.CopyToAsync(originalBodyStream);
                }
                // Restore the original response body stream
                context.Response.Body = originalBodyStream;
            }
        }
    }
}
