using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.fss
{
    public class BatchExpiryEndpoint: ServiceEndpointMock
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapPut("/batch/{batchId}/expiry", (string batchId, HttpRequest request, HttpResponse response) =>
            {
                EchoHeaders(request, response, [WellKnownHeader.CorrelationId]);
                var state = GetState(request);

                var rawRequestBody = new StreamReader(request.Body).ReadToEnd();
                if (string.IsNullOrEmpty(rawRequestBody))
                {
                    var errorObj = new
                    {
                        message = "Body Required expiry date",
                        expiryDate = "2022-03-15T12:57:39.896Z"
                    };

                    return Results.BadRequest(errorObj);
                }
                

                switch (state)
                {
                    case WellKnownState.Default:
                        return Results.NoContent();

                    case WellKnownState.BadRequest:
                        return Results.Json(new
                        {
                            correlationId = request.Headers[WellKnownHeader.CorrelationId],
                            errors = new[]
                            {
                                    new
                                    {
                                        source = "Batch expiry",
                                        description = "Batch ID does not exist or invalid date."
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
                    d.Append(new MarkdownHeader("Sets batch expiry date", 3));
                    d.Append(new MarkdownParagraph("Just returns a 204, won't actually set expiry"));
                });

    }

}
