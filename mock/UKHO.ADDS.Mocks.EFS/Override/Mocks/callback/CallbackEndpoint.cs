using System.Text;
using System.Text.Json;
using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.callback
{
    public class CallbackEndpoint : ServiceEndpointMock
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapPost("/callback", async (HttpRequest request, HttpResponse response) =>
            {
                EchoHeaders(request, response, [WellKnownHeader.CorrelationId]);
                var state = GetState(request);

                // Read the request body
                using var reader = new StreamReader(request.Body);
                var requestBody = await reader.ReadToEndAsync();

                if (string.IsNullOrEmpty(requestBody))
                {
                    return Results.BadRequest("Body required");
                }

                switch (state)
                {
                    case WellKnownState.Default:

                        var fileSystem = GetFileSystem();

                        try
                        {
                            // Create the callback directory if it doesn't exist
                            fileSystem.CreateDirectory("/callback-responses");

                            var fssBatchId = GetFssBatchId(requestBody);
                            
                            // Generate a filename with timestamp
                            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                            var fileName = string.IsNullOrEmpty(fssBatchId)
                                ? $"callback-response-{timestamp}.json"
                                : $"callback-response-{fssBatchId}.json";
                            var filePath = $"/callback-responses/{fileName}";

                            // Save the callback request to a file
                            using var file = fileSystem.OpenFile(filePath, FileMode.Create, FileAccess.Write, FileShare.Write);
                            var requestBytes = Encoding.UTF8.GetBytes(requestBody);
                            file.Write(requestBytes, 0, requestBytes.Length);
                            file.Flush();
                        }
                        catch (Exception ex)
                        {
                            return Results.BadRequest($"Failed to save callback response: {ex.Message}");
                        }

                        // Return 200 OK for successful callback
                        return Results.Ok(new
                        {
                            message = "Callback received successfully",
                            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                            status = "accepted"
                        });

                    case WellKnownState.BadRequest:
                        return Results.Json(new
                        {
                            correlationId = request.Headers[WellKnownHeader.CorrelationId],
                            errors = new[]
                            {
                                new
                                {
                                    source = "Callback Endpoint",
                                    description = "Invalid callback data format."
                                }
                            }
                        }, statusCode: 400);

                    case WellKnownState.Unauthorized:
                        return Results.Json(new
                        {
                            correlationId = request.Headers[WellKnownHeader.CorrelationId],
                            errors = new[]
                            {
                                new
                                {
                                    source = "Callback Endpoint",
                                    description = "Unauthorized access to callback endpoint."
                                }
                            }
                        }, statusCode: 401);

                    case WellKnownState.Forbidden:
                        return Results.Json(new
                        {
                            correlationId = request.Headers[WellKnownHeader.CorrelationId],
                            errors = new[]
                            {
                                new
                                {
                                    source = "Callback Endpoint",
                                    description = "Access to callback endpoint is forbidden."
                                }
                            }
                        }, statusCode: 403);

                    case WellKnownState.NotFound:
                        return Results.Json(new
                        {
                            correlationId = request.Headers[WellKnownHeader.CorrelationId],
                            details = "Callback endpoint not found"
                        }, statusCode: 404);

                    case WellKnownState.Conflict:
                        return Results.Json(new
                        {
                            correlationId = request.Headers[WellKnownHeader.CorrelationId],
                            errors = new[]
                            {
                                new
                                {
                                    source = "Callback Endpoint",
                                    description = "Callback processing conflict - duplicate callback received."
                                }
                            }
                        }, statusCode: 409);

                    case WellKnownState.UnsupportedMediaType:
                        return Results.Json(new
                        {
                            type = "https://httpstatuses.com/415",
                            title = "Unsupported Media Type",
                            status = 415,
                            detail = "The callback endpoint only accepts application/json content type.",
                            traceId = "00-012-0123-01"
                        }, statusCode: 415);

                    case WellKnownState.TooManyRequests:
                        return Results.Json(new
                        {
                            correlationId = request.Headers[WellKnownHeader.CorrelationId],
                            errors = new[]
                            {
                                new
                                {
                                    source = "Callback Endpoint",
                                    description = "Too many callback requests. Rate limit exceeded."
                                }
                            },
                            retryAfter = "60"
                        }, statusCode: 429);

                    case WellKnownState.InternalServerError:
                        return Results.Json(new
                        {
                            correlationId = request.Headers[WellKnownHeader.CorrelationId],
                            details = "Internal Server Error processing callback"
                        }, statusCode: 500);

                    case WellKnownState.Gone:
                        return Results.Json(new
                        {
                            correlationId = request.Headers[WellKnownHeader.CorrelationId],
                            errors = new[]
                            {
                                new
                                {
                                    source = "Callback Endpoint",
                                    description = "Callback endpoint is no longer available."
                                }
                            }
                        }, statusCode: 410);

                    case WellKnownState.PayloadTooLarge:
                        return Results.Json(new
                        {
                            correlationId = request.Headers[WellKnownHeader.CorrelationId],
                            errors = new[]
                            {
                                new
                                {
                                    source = "Callback Endpoint",
                                    description = "Callback payload exceeds maximum allowed size."
                                }
                            }
                        }, statusCode: 413);

                    case WellKnownState.ImATeapot:
                        return Results.Json(new
                        {
                            correlationId = request.Headers[WellKnownHeader.CorrelationId],
                            message = "I'm a teapot! Cannot process coffee callback requests.",
                            tip = "Try sending tea-related callbacks instead."
                        }, statusCode: 418);

                    default:
                        return WellKnownStateHandler.HandleWellKnownState(state);
                }
            })
                .Produces<string>()
                .WithEndpointMetadata(endpoint, d =>
                {
                    d.Append(new MarkdownHeader("Mock Callback Endpoint", 3));
                    d.Append(new MarkdownParagraph("Receives callback notifications and saves them to text files"));
                    d.Append(new MarkdownParagraph("Returns 200 OK for successful callbacks"));
                    d.Append(new MarkdownParagraph("**Available States:**"));
                    d.Append(new MarkdownParagraph("- `default`: Successfully processes callback and saves to file"));
                    d.Append(new MarkdownParagraph("- `badrequest`: Returns 400 for invalid callback data"));
                    d.Append(new MarkdownParagraph("- `unauthorized`: Returns 401 for unauthorized access"));
                    d.Append(new MarkdownParagraph("- `forbidden`: Returns 403 for forbidden access"));
                    d.Append(new MarkdownParagraph("- `notfound`: Returns 404 for endpoint not found"));
                    d.Append(new MarkdownParagraph("- `conflict`: Returns 409 for duplicate callbacks"));
                    d.Append(new MarkdownParagraph("- `unsupportedmediatype`: Returns 415 for wrong content type"));
                    d.Append(new MarkdownParagraph("- `toomanyrequests`: Returns 429 for rate limiting"));
                    d.Append(new MarkdownParagraph("- `internalservererror`: Returns 500 for server errors"));
                    d.Append(new MarkdownParagraph("- `gone`: Returns 410 for unavailable endpoint"));
                    d.Append(new MarkdownParagraph("- `payloadtoolarge`: Returns 413 for oversized payloads"));
                    d.Append(new MarkdownParagraph("- `imateapot`: Returns 418 for fun testing"));
                });

        /// <summary>
        /// Extracts the fssBatchId from the callback JSON payload.
        /// </summary>
        /// <param name="requestBody">The JSON request body as a string</param>
        /// <returns>The fssBatchId if found, otherwise an empty string</returns>
        private static string GetFssBatchId(string requestBody)
        {
            try
            {
                using var document = JsonDocument.Parse(requestBody);
                if (document.RootElement.TryGetProperty("data", out var dataElement) &&
                    dataElement.TryGetProperty("fssBatchId", out var batchIdElement))
                {
                    return batchIdElement.GetString() ?? string.Empty;
                }
                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
