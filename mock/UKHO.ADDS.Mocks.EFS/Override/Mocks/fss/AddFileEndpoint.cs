using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.fss
{
    public class AddFileEndpoint : FssEndpointBase
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapPost("/batch/{batchId}/files/{fileName}", (string batchId, string fileName, HttpRequest request, HttpResponse response) =>
                {
                    var (state, correlationId) = SetupRequest(request, response);
                    
                    // Prepare storage for the file blocks
                    EnsureS100DirectoryExists($"{fileName}_blocks");

                    return state switch
                    {
                        WellKnownState.Default => Results.Created(),
                        WellKnownState.BadRequest => Results.Json(CreateErrorResponse(correlationId, "Add File", "Batch ID is missing in the URI."), statusCode: 400),
                        _ => ProcessCommonStates(state, correlationId, "Add File") ?? WellKnownStateHandler.HandleWellKnownState(state)
                    };
                })
                .Produces<string>()
                .WithEndpointMetadata(endpoint, d =>
                {
                    d.Append(new MarkdownHeader("Adds a file", 3));
                    d.Append(new MarkdownParagraph("Registers a file in the batch and prepares for block uploads"));
                });
    }
}
