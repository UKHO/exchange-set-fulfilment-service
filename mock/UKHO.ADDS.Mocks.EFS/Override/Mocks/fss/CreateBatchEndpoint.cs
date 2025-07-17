using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.fss
{
    public class CreateBatchEndpoint : ServiceEndpointMock
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapPost("/batch", (HttpRequest request, HttpResponse response) =>
                {
                    EchoHeaders(request, response, [WellKnownHeader.CorrelationId]);
                    var state = GetState(request);

                    var rawRequestBody = new StreamReader(request.Body).ReadToEnd();
                    if (string.IsNullOrEmpty(rawRequestBody))
                    {
                        return Results.BadRequest("Body required");
                    }

                    switch (state)
                    {
                        case WellKnownState.Default:

                            var newBatch = new { batchId = Guid.NewGuid().ToString() };
                            return Results.Created("", newBatch);

                        case WellKnownState.BadRequest:
                            return Results.Json(new
                            {
                                correlationId = request.Headers[WellKnownHeader.CorrelationId],
                                errors = new[]
                                {
                                    new
                                    {
                                        source = "Create Batch",
                                        description = "Invalid Expiry Date Format."
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
                    d.Append(new MarkdownHeader("Create a batch", 3));
                    d.Append(new MarkdownParagraph("Just returns a 201, won't actually create anything"));
                });
    }
}
