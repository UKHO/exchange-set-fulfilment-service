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
                    EchoHeaders(request, response, [WellKnownHeader.CorrelationId]);
                    var state = GetState(request);
                    var correlationId = GetCorrelationId(request);
                    
                    // Prepare storage for the file blocks
                    var fileSystem = GetFileSystem();
                    try
                    {
                        fileSystem.CreateDirectory("/S100-ExchangeSets");
                        // Create a directory to store blocks for this file
                        fileSystem.CreateDirectory($"/S100-ExchangeSets/{fileName}_blocks");
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
                            return Results.Json(CreateErrorResponse(correlationId, "Add File", "Batch ID is missing in the URI."), statusCode: 400);

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
                    d.Append(new MarkdownHeader("Adds a file", 3));
                    d.Append(new MarkdownParagraph("Registers a file in the batch and prepares for block uploads"));
                });
    }
}
