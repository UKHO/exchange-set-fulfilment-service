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

                var blockRequestResult = ParseBlockRequest(request);
                if (blockRequestResult.IsError)
                {
                    return blockRequestResult.ErrorResult;
                }

                var validationResult = ValidateBlockRequest(blockRequestResult.BlockRequest);
                if (validationResult != null)
                {
                    return validationResult;
                }

                var assemblyResult = AssembleFileFromBlocks(batchId, filename, blockRequestResult.BlockRequest, correlationId);
                if (assemblyResult != null)
                {
                    return assemblyResult;
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

        private static (WriteBlockRequest BlockRequest, bool IsError, IResult ErrorResult) ParseBlockRequest(HttpRequest request)
        {
            try
            {
                using var streamReader = new StreamReader(request.Body);
                var requestBody = streamReader.ReadToEnd();
                var blockRequest = JsonSerializer.Deserialize<WriteBlockRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new WriteBlockRequest();
                return (blockRequest, false, null!);
            }
            catch
            {
                return (null!, true, Results.BadRequest("Invalid request format. Expected a JSON object with blockIds array."));
            }
        }

        private static IResult? ValidateBlockRequest(WriteBlockRequest blockRequest)
        {
            if (blockRequest.BlockIds == null || !blockRequest.BlockIds.Any())
            {
                return Results.BadRequest("No block IDs provided in the request.");
            }
            return null;
        }

        private IResult? AssembleFileFromBlocks(string batchId, string filename, WriteBlockRequest blockRequest, string correlationId)
        {
            var fileKey = $"{batchId}:{filename}";

            try
            {
                if (!FileBlockStorage.FileBlocks.ContainsKey(fileKey))
                {
                    return Results.BadRequest($"No blocks found for file {filename}");
                }

                var fileBlocks = FileBlockStorage.FileBlocks[fileKey];
                var assembledData = AssembleBlocksInOrder(blockRequest.BlockIds, fileBlocks, filename);
                if (assembledData.IsError)
                {
                    return assembledData.ErrorResult;
                }

                WriteAssembledFileToStorage(filename, assembledData.Data);
                FileBlockStorage.FileBlocks.Remove(fileKey);
                
                return null; // Success, no error result
            }
            catch (Exception ex)
            {
                return Results.Json(CreateDetailsResponse(correlationId, $"Failed to assemble file: {ex.Message}"), statusCode: 500);
            }
        }

        private static (byte[] Data, bool IsError, IResult ErrorResult) AssembleBlocksInOrder(IEnumerable<string> blockIds, Dictionary<string, byte[]> fileBlocks, string filename)
        {
            using var assembledFile = new MemoryStream();

            foreach (var blockId in blockIds.OrderBy(id => id))
            {
                if (!fileBlocks.TryGetValue(blockId, out var blockData))
                {
                    return (null!, true, Results.BadRequest($"Block {blockId} not found for file {filename}"));
                }
                assembledFile.Write(blockData, 0, blockData.Length);
            }

            return (assembledFile.ToArray(), false, null!);
        }

        private void WriteAssembledFileToStorage(string filename, byte[] assembledData)
        {
            try
            {
                var fileSystem = GetFileSystem();
                fileSystem.CreateDirectory(S100ExchangeSetsPath);
                using var finalFile = fileSystem.OpenFile($"{S100ExchangeSetsPath}/{filename}", FileMode.Create, FileAccess.Write, FileShare.None);
                finalFile.Write(assembledData, 0, assembledData.Length);
                finalFile.Flush();
            }
            catch(Exception)
            {
                // Log the error but continue - we might still be able to succeed
            }
        }

        private static object CreateBodyRequiredError() => new
        {
            message = "Body required with one or more",
            blockIds = new[] { "00001" }
        };
    }
}
