using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;
using System.Text;

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

                            // Generate a filename with timestamp
                            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
                            var fileName = $"callback-response-{timestamp}.json";
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
                                    description = "Invalid callback data."
                                }
                            }
                        }, statusCode: 400);

                    case WellKnownState.NotFound:
                        return Results.Json(new
                        {
                            correlationId = request.Headers[WellKnownHeader.CorrelationId],
                            details = "Callback endpoint not found"
                        }, statusCode: 404);

                    case WellKnownState.InternalServerError:
                        return Results.Json(new
                        {
                            correlationId = request.Headers[WellKnownHeader.CorrelationId],
                            details = "Internal Server Error"
                        }, statusCode: 500);

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
                });
    }
}
