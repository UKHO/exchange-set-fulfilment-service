using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.callback
{
    /// <summary>
    /// Mock callback endpoint for receiving Exchange Set notifications
    /// </summary>
    public class CallbackEndpoint : ServiceEndpointMock
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapPost("/callback", async (HttpRequest request) =>
            {
                var state = GetState(request);

                switch (state)
                {
                    case WellKnownState.Default:
                        try
                        {
                            // Read the request body
                            string requestBody;
                            using (var reader = new StreamReader(request.Body))
                            {
                                requestBody = await reader.ReadToEndAsync();
                            }

                            // Log the received callback data
                            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}] Received callback notification:");
                            Console.WriteLine(requestBody);

                            // Save to FileShare Service file system using GetFileSystem() like UploadBlockEndpoint
                            await SaveCallbackToFileShareSystem(requestBody, request);

                            // Save the callback data to a local file (existing functionality)
                            var fileName = $"callback_response_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.txt";
                            var filePath = System.IO.Path.Combine("callback_responses", fileName);

                            // Ensure the directory exists
                            Directory.CreateDirectory("callback_responses");

                            // Write the response to file
                            await File.WriteAllTextAsync(filePath, requestBody);

                            Console.WriteLine($"Callback response saved to: {filePath}");

                            // Return successful response
                            return Results.Ok(new {
                                message = "Callback received successfully",
                                timestamp = DateTime.UtcNow,
                                savedTo = fileName
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing callback: {ex.Message}");
                            return Results.Problem("Internal server error processing callback");
                        }

                    default:
                        // Just send default responses
                        return WellKnownStateHandler.HandleWellKnownState(state);
                }
            })
                .Produces<object>()
                .WithEndpointMetadata(endpoint, d =>
                {
                    d.Append(new MarkdownHeader("Mock Callback Endpoint", 3));
                    d.Append(new MarkdownParagraph("Receives CloudEvents callback notifications and saves them to FileShare Service file system and local file"));
                });

        private async Task SaveCallbackToFileShareSystem(string content, HttpRequest request)
        {
            try
            {
                // Get the file system like UploadBlockEndpoint does
                var fileSystem = GetFileSystem();

                // Create the callback responses directory if it doesn't exist
                fileSystem.CreateDirectory("/Callback-Responses");

                // Generate a unique filename for the callback response
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
                var callbackFileName = $"callback_{timestamp}.json";
                var filePath = "/Callback-Responses/" + callbackFileName;

                // Open file for writing like UploadBlockEndpoint does
                using var file = fileSystem.OpenFile(filePath, FileMode.Create, FileAccess.Write, FileShare.Write);

                // Convert string content to bytes and write to file
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
                await file.WriteAsync(contentBytes, 0, contentBytes.Length);
                file.Flush();

                // Log the file save operation
                var correlationId = request.Headers.TryGetValue("x-correlation-id", out var headerValues)
                    ? headerValues.FirstOrDefault() ?? "unknown"
                    : "unknown";

                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}] Saved callback AC response to FileShare Service:");
                Console.WriteLine($"File: {filePath}, CorrelationId: {correlationId}");
                Console.WriteLine($"Content length: {content.Length} characters");

                // Check if this is CloudEvents format
                if (content.Contains("\"specversion\"") && content.Contains("\"type\""))
                {
                    Console.WriteLine("CloudEvents format detected in saved AC response");
                    if (content.Contains("uk.co.admiralty.avcsData.exchangeSetCreated"))
                    {
                        Console.WriteLine("Exchange Set Created event type confirmed");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save callback AC response to FileShare Service file system: {ex.Message}");
                // Don't throw - we don't want to break the callback response
            }
        }
    }
}
