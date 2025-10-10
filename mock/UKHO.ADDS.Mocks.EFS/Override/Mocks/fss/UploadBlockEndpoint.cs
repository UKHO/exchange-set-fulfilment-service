using UKHO.ADDS.Mocks.EFS.Override.Mocks.fss.Models;
using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.fss
{
    public class UploadBlockEndpoint : ServiceEndpointMock
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapPut("/batch/{batchId}/files/{fileName}/{blockId}", (string batchId, string filename, string blockId, HttpRequest request, HttpResponse response) =>
            {
                EchoHeaders(request, response, [WellKnownHeader.CorrelationId]);
                var state = GetState(request);

                if (request.Body.Length == 0)
                {
                    return Results.BadRequest("Body required");
                }

                try
                {
                    // Create a composite key for the file
                    var fileKey = $"{batchId}:{filename}";
                    
                    // Initialize dictionary for this file if it doesn't exist
                    lock (FileBlockStorage.FileBlocks)
                    {
                        if (!FileBlockStorage.FileBlocks.ContainsKey(fileKey))
                        {
                            FileBlockStorage.FileBlocks[fileKey] = new Dictionary<string, byte[]>();
                        }
                        
                        // Read the block content
                        using var ms = new MemoryStream();
                        request.Body.CopyTo(ms);
                        
                        // Store the block
                        FileBlockStorage.FileBlocks[fileKey][blockId] = ms.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex.Message);
                }

                // Also save to filesystem as a backup/alternative access method
                var fileSystem = GetFileSystem();
                try
                {
                    fileSystem.CreateDirectory("/S100-ExchangeSets");
                }
                catch (Exception)
                {
                    // Ignore directory creation errors
                }

                switch (state)
                {
                    case WellKnownState.Default:
                        return Results.Created();

                    case WellKnownState.BadRequest:
                        return Results.Json(new
                        {
                            correlationId = request.Headers[WellKnownHeader.CorrelationId],
                            errors = new[]
                            {
                                new
                                {
                                    source = "Upload Block",
                                    description = "Invalid batchId."
                                }
                            }
                        }, statusCode: 400);

                    case WellKnownState.NotFound:
                        return Results.Json(new
                        {
                            correlationId = request.Headers[WellKnownHeader.CorrelationId],
                            details = "Not Found"
                        }, statusCode: 404);

                    case WellKnownState.UnsupportedMediaType:
                        return Results.Json(new
                        {
                            type = "https://example.com",
                            title = "Unsupported Media Type",
                            status = 415,
                            traceId = "00-012-0123-01"
                        }, statusCode: 415);

                    case WellKnownState.InternalServerError:
                        return Results.Json(new
                        {
                            correlationId = request.Headers[WellKnownHeader.CorrelationId],
                            details = "Internal Server Error"
                        }, statusCode: 500);

                    default:
                        // Just send default responses
                        return WellKnownStateHandler.HandleWellKnownState(state);
                }
            })
                .Produces<string>()
                .WithEndpointMetadata(endpoint, d =>
                {
                    d.Append(new MarkdownHeader("Upload a file block", 3));
                    d.Append(new MarkdownParagraph("Stores each file block in memory for assembly during WriteBlock operation"));
                });
    }
}
