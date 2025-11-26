using UKHO.ADDS.Mocks.EFS.Override.Mocks.fss.ResponseGenerator;
using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.fss
{
    public class GetFssBatchesEndpoint : ServiceEndpointMock
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapGet("/batch", (HttpRequest request) =>
                {
                    var state = GetState(request);

                    switch (state)
                    {
                        case WellKnownState.Default:

                            return FssResponseGenerator.ProvideSearchFilterResponse(request);

                        case WellKnownState.BadRequest:
                            return CreateErrorResponse(
                                request,
                                400,
                                new
                                {
                                    errors = new[]
                                    {
                                        new
                                        {
                                            source = "Search Batch",
                                            description = "Bad Request."
                                        }
                                    }
                                }
                            );

                        case WellKnownState.NotFound:
                            return CreateErrorResponse(
                                request,
                                404,
                                new { details = "Not Found" }
                            );

                        case WellKnownState.UnsupportedMediaType:
                            return Results.Json(new
                            {
                                type = "https://example.com",
                                title = "Unsupported Media Type",
                                status = 415,
                                traceId = "00-012-0123-01"
                            }, statusCode: 415);

                        case WellKnownState.InternalServerError:
                            return CreateErrorResponse(
                                request,
                                500,
                                new { details = "Internal Server Error" }
                            );                            

                        default:
                            // Just send default responses
                            return WellKnownStateHandler.HandleWellKnownState(state);
                    }
                })
                .Produces<string>()
                .WithEndpointMetadata(endpoint, d =>
                {
                    d.Append(new MarkdownHeader("Gets Batches", 3));
                    d.Append(new MarkdownParagraph(new MarkdownEmphasis("This is driven from Response Generator Process")));

                    d.Append(new MarkdownParagraph("S-100 Sample query attributes:"));
                    d.Append(new MarkdownList(
                        new MarkdownTextListItem("Key $Filter"),
                        new MarkdownTextListItem("Value BusinessUnit eq 'ADDS-S100' and $batch(Product Type) eq 'S-100' and  (($batch(Product Name) eq '101GB004DEVQK' and $batch(Edition Number) eq '2' and (($batch(Update Number) eq '0' or $batch(Update Number) eq '1' ))))")
                    ));
                });

        private static IResult CreateErrorResponse(HttpRequest request, int statusCode, object errorBody)
        {
            var correlationId = request.Headers[WellKnownHeader.CorrelationId];
            var response = new Dictionary<string, object>
            {
                ["correlationId"] = correlationId
            };

            // Merge errorBody properties into the response dictionary
            foreach (var prop in errorBody.GetType().GetProperties())
            {
                response[prop.Name] = prop.GetValue(errorBody);
            }

            return Results.Json(response, statusCode: statusCode);
        }
    }
}
