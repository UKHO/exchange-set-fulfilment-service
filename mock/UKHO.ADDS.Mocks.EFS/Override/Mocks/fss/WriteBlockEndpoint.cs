using System.Text.Json;
using UKHO.ADDS.Mocks.EFS.Override.Mocks.fss.Models;
using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.fss
{
    public class WriteBlockEndpoint : FssEndpointBase
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapPut("/batch/{batchId}/files/{fileName}", (string batchId, string filename, HttpRequest request, HttpResponse response) =>
            {
                EchoHeaders(request, response, [WellKnownHeader.CorrelationId]);
                var state = GetState(request);
                var correlationId = GetCorrelationId(request);

                if (request.Body.Length == 0)
                {
                    return Results.BadRequest(CreateBodyRequiredError());
                }

                // Try to read the block IDs from the request
                WriteBlockRequest blockRequest;
                try
                {
                    using var streamReader = new StreamReader(request.Body);
                    var requestBody = streamReader.ReadToEnd();
                    blockRequest = JsonSerializer.Deserialize<WriteBlockRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new WriteBlockRequest();
                }
                catch
                {
                    return Results.BadRequest("Invalid request format. Expected a JSON object with blockIds array.");
                }

                // Check if the blockIds are present
                if (blockRequest.BlockIds == null || !blockRequest.BlockIds.Any())
                {
                    return Results.BadRequest("No block IDs provided in the request.");
                }

                var fileSystem = GetFileSystem();
                var fileKey = $"{batchId}:{filename}";

                try
                {
                    // Check if we have blocks for this file
                    if (!FileBlockStorage.FileBlocks.ContainsKey(fileKey))
                    {
                        return Results.BadRequest($"No blocks found for file {filename}");
                    }

                    var fileBlocks = FileBlockStorage.FileBlocks[fileKey];

                    // Create a memory stream to assemble the complete file
                    using var assembledFile = new MemoryStream();

                    // Process blocks in order
                    foreach (var blockId in blockRequest.BlockIds.OrderBy(id => id))
                    {
                        if (!fileBlocks.TryGetValue(blockId, out var blockData))
                        {
                            return Results.BadRequest($"Block {blockId} not found for file {filename}");
                        }

                        assembledFile.Write(blockData, 0, blockData.Length);
                    }

                    // Write the assembled file to the file system
                    try
                    {
                        fileSystem.CreateDirectory("/S100-ExchangeSets");
                        using var finalFile = fileSystem.OpenFile($"/S100-ExchangeSets/{filename}", FileMode.Create, FileAccess.Write, FileShare.None);
                        assembledFile.Position = 0;
                        assembledFile.CopyTo(finalFile);
                        finalFile.Flush();
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue - we might still be able to succeed
                        Console.WriteLine($"Error writing to file system: {ex.Message}");
                    }
                    // Clean up the blocks to free memory
                    FileBlockStorage.FileBlocks.Remove(fileKey);
                }
                catch (Exception ex)
                {
                    return Results.Json(CreateDetailsResponse(correlationId, $"Failed to assemble file: {ex.Message}"), statusCode: 500);
                }

                switch (state)
                {
                    case WellKnownState.Default:
                        return Results.NoContent();
                    case WellKnownState.BadRequest:
                        return Results.Json(CreateErrorResponse(correlationId, "Write Block", "Invalid BatchId"), statusCode: 400);
                    case WellKnownState.NotFound:
                        return Results.Json(CreateDetailsResponse(correlationId, "Not Found"), statusCode: 404);
                    case WellKnownState.UnsupportedMediaType:
                        return Results.Json(CreateUnsupportedMediaTypeResponse(), statusCode: 415);
                    case WellKnownState.InternalServerError:
                        return Results.Json(CreateDetailsResponse(correlationId, InternalServerErrorMessage), statusCode: 500);
                    default:
                        // Just send default responses
                        return WellKnownStateHandler.HandleWellKnownState(state);
                }
            })
                .Produces<string>()
                .WithEndpointMetadata(endpoint, d =>
                {
                    d.Append(new MarkdownHeader("Write a file block", 3));
                    d.Append(new MarkdownParagraph("Assembles the file from uploaded blocks and creates the final file"));
                });

        private static object CreateBodyRequiredError() => new
        {
            message = "Body required with one or more",
            blockIds = new[] { "00001" }
        };
    }
}
