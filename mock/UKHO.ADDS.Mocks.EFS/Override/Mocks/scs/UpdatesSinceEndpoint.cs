using UKHO.ADDS.Mocks.Configuration.Mocks.scs.ResponseGenerator;
using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.Configuration.Mocks.scs
{
    public class UpdatesSinceEndpoint : ServiceEndpointMock
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapGet("/v2/products/s100/updatesSince", (string sinceDateTime, string? productIdentifier, HttpRequest request, HttpResponse response) =>
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
                                !acceptHeader.Contains("*/*") &&
                                acceptHeader.Contains("application/xml"))
                            {
                                return Results.Json(new
                                {
                                    type = "https://tools.ietf.org/html/rfc9110#section-15.5.16",
                                    title = "Unsupported Media Type",
                                    status = 415,
                                    traceId = Guid.NewGuid().ToString("D")[..23]
                                }, statusCode: 415);
                            }

                            if (string.IsNullOrWhiteSpace(sinceDateTime))
                            {
                                return Results.Json(new
                                {
                                    correlationId = request.Headers[WellKnownHeader.CorrelationId],
                                    errors = new[]
                                    {
                                    new
                                    {
                                        source = "sinceDateTime",
                                        description = "The sinceDateTime query parameter is required."
                                    }
                                }
                                }, statusCode: 400);
                            }

                            if (!DateTime.TryParse(sinceDateTime, out var parsedSinceDateTime))
                            {
                                return Results.Json(new
                                {
                                    correlationId = request.Headers[WellKnownHeader.CorrelationId],
                                    errors = new[]
                                    {
                                    new
                                    {
                                        source = "sinceDateTime",
                                        description = "Provided date format is not valid."
                                    }
                                }
                                }, statusCode: 400);
                            }

                            response.GetTypedHeaders().LastModified = DateTime.UtcNow;

                            // Use the same file access pattern as GetBasicCatalogueEndpoint
                            var pathResult = GetFile("s100-updates-since.json");
                            if (pathResult.IsSuccess(out var file))
                            {
                                // Call the response generator with the file
                                var task = ScsResponseGenerator.ProvideUpdatesSinceResponse(parsedSinceDateTime, productIdentifier, request, file);
                                return task.GetAwaiter().GetResult();
                            }

                            return Results.NotFound("Could not find s100-updates-since.json file");
                        }

                    case WellKnownState.NotModified:
                        response.GetTypedHeaders().LastModified = DateTime.UtcNow;
                        return Results.StatusCode(304);

                    case WellKnownState.BadRequest:
                        return Results.Json(new
                        {
                            correlationId = request.Headers[WellKnownHeader.CorrelationId],
                            errors = new[]
                            {
                                new
                                {
                                    source = "Updates Since",
                                    description = "Provided date format is not valid."
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
                            type = "https://tools.ietf.org/html/rfc9110#section-15.5.16",
                            title = "Unsupported Media Type",
                            status = 415,
                            traceId = Guid.NewGuid().ToString("D")[..23]
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
                    d.Append(new MarkdownHeader("Updates Since Endpoint", 3));
                    d.Append(new MarkdownParagraph("This endpoint returns all releasable changes to products since a specified date."));
                    d.Append(new MarkdownParagraph("**Required Parameter:** sinceDateTime - The date and time from which changes are requested (ISO 8601 format)."));
                    d.Append(new MarkdownParagraph("**Optional Parameter:** productIdentifier - Filter by S-100 standard specification (e.g., 's101')."));
                    d.Append(new MarkdownParagraph(new MarkdownEmphasis("Note: The list is static in nature loaded from s100-updates-since.json. Only one identifier at a time is allowed (e.g., s101) to get 101 products.")));
                });
    }
}
