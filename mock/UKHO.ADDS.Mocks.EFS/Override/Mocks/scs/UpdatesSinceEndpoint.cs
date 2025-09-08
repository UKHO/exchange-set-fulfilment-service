using System.Globalization;
using UKHO.ADDS.Mocks.Configuration.Mocks.scs.Helpers;
using UKHO.ADDS.Mocks.Configuration.Mocks.scs.ResponseGenerator;
using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.Configuration.Mocks.scs
{
    public class UpdatesSinceEndpoint : ServiceEndpointMock
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapGet("/v2/products/s100/updatesSince", async (string sinceDateTime, string? productIdentifier, HttpRequest request, HttpResponse response) =>
            {
                EchoHeaders(request, response, [WellKnownHeader.CorrelationId]);

                var state = GetState(request);

                switch (state)
                {
                    case WellKnownState.Default:
                        {
                            // Check Accept header for unsupported media types (415)
                            var acceptHeader = request.Headers.Accept.ToString();
                            if (!string.IsNullOrEmpty(acceptHeader) &&
                                !acceptHeader.Contains("application/json") &&
                                !acceptHeader.Contains("*/*"))
                            {
                                return ResponseHelper.CreateUnsupportedMediaTypeResponse();
                            }

                            if (string.IsNullOrWhiteSpace(sinceDateTime))
                            {
                                return ResponseHelper.CreateBadRequestResponse(request, "sinceDateTime", "The sinceDateTime query parameter is required.");
                            }

                            if (!DateTime.TryParse(sinceDateTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedSinceDateTime))
                            {
                                return ResponseHelper.CreateBadRequestResponse(request, "sinceDateTime", "Provided date format is not valid.");
                            }

                            response.GetTypedHeaders().LastModified = DateTime.UtcNow;

                            // Use the same file access pattern as GetBasicCatalogueEndpoint
                            var pathResult = GetFile("s100-updates-since.json");
                            if (pathResult.IsSuccess(out var file))
                            {
                                // Call the response generator with the file
                                return await ScsResponseGenerator.ProvideUpdatesSinceResponse(parsedSinceDateTime, productIdentifier, request, file);
                            }

                            return Results.NotFound("Could not find s100-updates-since.json file");
                        }

                    case WellKnownState.NotModified:
                        response.GetTypedHeaders().LastModified = DateTime.UtcNow;
                        return Results.StatusCode(304);

                    case WellKnownState.BadRequest:
                        return ResponseHelper.CreateBadRequestResponse(request, "Updates Since", "Provided date format is not valid.");

                    case WellKnownState.NotFound:
                        return ResponseHelper.CreateNotFoundResponse(request);

                    case WellKnownState.UnsupportedMediaType:
                        return ResponseHelper.CreateUnsupportedMediaTypeResponse();

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
                    d.Append(new MarkdownHeader("Updates Since Endpoint", 3));
                    d.Append(new MarkdownParagraph("This endpoint returns all releasable changes to products since a specified date."));
                    d.Append(new MarkdownParagraph("**Required Parameter:** sinceDateTime - The date and time from which changes are requested (ISO 8601 format)."));
                    d.Append(new MarkdownParagraph("**Optional Parameter:** productIdentifier - Filter by S-100 standard specification (e.g., 's101')."));
                    d.Append(new MarkdownParagraph(new MarkdownEmphasis("Note: The list is static in nature loaded from s100-updates-since.json. Only one identifier at a time is allowed (e.g., s101) to get 101 products.")));
                });
    }
}
