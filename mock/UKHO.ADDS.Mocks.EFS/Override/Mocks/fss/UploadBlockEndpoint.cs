using UKHO.ADDS.Mocks.EFS.Override.Mocks.fss.Models;
using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.fss
{
    public class UploadBlockEndpoint : FssEndpointBase
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapPut("/batch/{batchId}/files/{fileName}/{blockId}", (string batchId, string filename, string blockId, HttpRequest request, HttpResponse response) =>
            {
                var (state, correlationId) = SetupRequest(request, response);

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
                EnsureS100DirectoryExists();

                return state switch
                {
                    WellKnownState.Default => Results.Created(),
                    WellKnownState.BadRequest => Results.Json(CreateErrorResponse(correlationId, "Upload Block", "Invalid batchId."), statusCode: 400),
                    _ => ProcessCommonStates(state, correlationId, "Upload Block") ?? WellKnownStateHandler.HandleWellKnownState(state)
                };
            })
                .Produces<string>()
                .WithEndpointMetadata(endpoint, d =>
                {
                    d.Append(new MarkdownHeader("Upload a file block", 3));
                    d.Append(new MarkdownParagraph("Stores each file block in memory for assembly during WriteBlock operation"));
                });
    }
}
