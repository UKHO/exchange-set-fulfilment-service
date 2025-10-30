using UKHO.ADDS.Mocks.EFS.Override.Mocks.fss.Models;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.fss
{
    public class UploadBlockEndpoint : FssEndpointBase
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapPut("/batch/{batchId}/files/{fileName}/{blockId}", async (string batchId, string filename, string blockId, HttpRequest request, HttpResponse response) =>
            {
                var (state, correlationId) = SetupRequest(request, response);

                if (request.Body.Length == 0)
                {
                    return Results.BadRequest("Body required");
                }

                var storageResult = await StoreBlockData(batchId, filename, blockId, request);
                if (storageResult != null)
                {
                    return storageResult;
                }

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

        private static async Task<IResult?> StoreBlockData(string batchId, string filename, string blockId, HttpRequest request)
        {
            try
            {
                var fileKey = $"{batchId}:{filename}";

                lock (FileBlockStorage.FileBlocks)
                {
                    EnsureFileKeyExists(fileKey);
                }
                var blockData = await ReadBlockContent(request);
                lock (FileBlockStorage.FileBlocks)
                {
                    FileBlockStorage.FileBlocks[fileKey][blockId] = blockData;
                }

                return null; // Success
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }

        private static void EnsureFileKeyExists(string fileKey)
        {
            if (!FileBlockStorage.FileBlocks.ContainsKey(fileKey))
            {
                FileBlockStorage.FileBlocks[fileKey] = new Dictionary<string, byte[]>();
            }
        }

        private static async Task<byte[]> ReadBlockContent(HttpRequest request)
        {
            var length = request.ContentLength ?? 0;
            if (length == 0)
                return [];

            var buffer = new byte[length];
            var read = 0;
            while (read < length)
            {
                var bytesRead = await request.Body.ReadAsync(buffer, read, (int)length - read);
                if (bytesRead == 0)
                    break;
                read += bytesRead;
            }
            return buffer;
        }
    }
}
