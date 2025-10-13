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
                var (state, correlationId) = SetupRequest(request, response);

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
                        var fileSystem = GetFileSystem();
                        fileSystem.CreateDirectory(S100ExchangeSetsPath);
                        using var finalFile = fileSystem.OpenFile($"{S100ExchangeSetsPath}/{filename}", FileMode.Create, FileAccess.Write, FileShare.None);
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

                return state switch
                {
                    WellKnownState.Default => Results.NoContent(),
                    WellKnownState.BadRequest => Results.Json(CreateErrorResponse(correlationId, "Write Block", "Invalid BatchId"), statusCode: 400),
                    _ => ProcessCommonStates(state, correlationId, "Write Block") ?? WellKnownStateHandler.HandleWellKnownState(state)
                };
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
