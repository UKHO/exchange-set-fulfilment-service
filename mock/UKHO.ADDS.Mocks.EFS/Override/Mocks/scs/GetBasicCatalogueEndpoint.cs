using UKHO.ADDS.Mocks.Configuration.Mocks.scs.Helpers;
using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.Mime;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.Configuration.Mocks.scs
{
    public class GetBasicCatalogueEndpoint : ServiceEndpointMock
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapGet("/v2/catalogues/{productType}/basic", (string productType, HttpRequest request, HttpResponse response) =>
                {
                    EchoHeaders(request, response, [WellKnownHeader.CorrelationId]);

                    var state = GetState(request);

                    switch (state)
                    {
                        case WellKnownState.Default:
                        {
                            switch (productType.ToLowerInvariant())
                            {
                                case "s100":
                                    var pathResult = GetFile("s100-catalogue.json");

                                    if (pathResult.IsSuccess(out var file))
                                    {
                                        response.GetTypedHeaders().LastModified = DateTime.UtcNow;
                                        return Results.File(file.Open(), file.MimeType);
                                    }

                                    return Results.NotFound("Could not find the path in the /files GET method");
                                default:
                                    return Results.BadRequest("No productType set");
                            }
                        }

                        case WellKnownState.NotModified:
                            response.GetTypedHeaders().LastModified = DateTime.UtcNow;
                            return Results.StatusCode(304);

                        case WellKnownState.BadRequest:
                            return ResponseHelper.CreateBadRequestResponse(request, "Basic Catalogue", "Provided date format is not valid.");

                        case WellKnownState.NotFound:
                            return ResponseHelper.CreateNotFoundResponse(request);

                        case WellKnownState.UnsupportedMediaType:
                            return ResponseHelper.CreateUnsupportedMediaTypeResponse("https://example.com", "00-012-0123-01");

                        case WellKnownState.InternalServerError:
                            return ResponseHelper.CreateInternalServerErrorResponse(request);

                        default:
                            // Just send default responses
                            return WellKnownStateHandler.HandleWellKnownState(state);
                    }
                })
                .Produces<string>()
                .WithEndpointMetadata(endpoint, d =>
                {
                    d.Append(new MarkdownHeader("Gets a basic catalog", 3));
                    d.Append(new MarkdownParagraph(new MarkdownEmphasis("Only s100 is implemented at the moment")));
                });
    }
}
