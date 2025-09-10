using UKHO.ADDS.Mocks.Configuration.Mocks.scs.Helpers;
using UKHO.ADDS.Mocks.Configuration.Mocks.scs.ResponseGenerator;
using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.Configuration.Mocks.scs
{
    public class ProductVersionsEndpoint : ServiceEndpointMock
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapPost("/v2/products/{productType}/ProductVersions", async (string productType, HttpRequest request, HttpResponse response) =>
            {
                EchoHeaders(request, response, [WellKnownHeader.CorrelationId]);

                var contentTypeHeader = request.Headers.ContentType.ToString();
                if (string.IsNullOrEmpty(contentTypeHeader) ||
                    !contentTypeHeader.Contains("application/json"))
                {
                    return ResponseHelper.CreateUnsupportedMediaTypeResponse();
                }

                var state = GetState(request);                

                switch (state)
                {
                    case WellKnownState.Default:
                        {                          
                            switch (productType.ToLowerInvariant())
                            {
                                case "s100":

                                    response.GetTypedHeaders().LastModified = DateTime.UtcNow;
                                    return await ScsResponseGenerator.ProvideProductVersionsResponse(request);

                                default:
                                    return ResponseHelper.CreateBadRequestResponse(request, "Product Versions", "Bad Request.");
                            }
                        }

                    case "get-invalidproducts":

                        response.GetTypedHeaders().LastModified = DateTime.UtcNow;
                        return await ScsResponseGenerator.ProvideProductVersionsResponse(request, state);

                    case "get-allinvalidproducts":

                        response.GetTypedHeaders().LastModified = DateTime.UtcNow;
                        return await ScsResponseGenerator.ProvideProductVersionsResponse(request, state);

                    case "get-cancelledproducts":

                        response.GetTypedHeaders().LastModified = DateTime.UtcNow;
                        return await ScsResponseGenerator.ProvideProductVersionsResponse(request, state);

                    case "get-productwithdrawn":

                        response.GetTypedHeaders().LastModified = DateTime.UtcNow;
                        return await ScsResponseGenerator.ProvideProductVersionsResponse(request, state);

                    case WellKnownState.NotModified:
                        response.GetTypedHeaders().LastModified = DateTime.UtcNow;
                        return Results.StatusCode(304);

                    case WellKnownState.BadRequest:
                        return ResponseHelper.CreateBadRequestResponse(request, "Product Versions", "Bad Request.");

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
                    d.Append(new MarkdownHeader("Product Versions Endpoint", 3));
                    d.Append(new MarkdownParagraph("This endpoint is used to retrieve the latest baseline, releasable versions for requested products since a specified version."));

                    d.Append(new MarkdownHeader("Try out the get-invalidproducts state!", 3));
                    d.Append(new MarkdownParagraph("The response mimics a situation where one of the requested products is unavailable. The final item in the request is omitted from the returned list and is instead flagged as 'not returned', along with a reason like 'invalidProduct'."));

                    d.Append(new MarkdownHeader("Try out the get-allinvalidproducts state!", 3));
                    d.Append(new MarkdownParagraph("The response mimics a situation where ALL requested products are invalid. No products are returned and all requested products are flagged as 'not returned' with reason 'invalidProduct'. This simulates the scenario where an error should be logged and no exchange set should be created."));

                    d.Append(new MarkdownHeader("Try out the get-cancelledproducts state!", 3));
                    d.Append(new MarkdownParagraph("The response mimics a situation where one of the requested products is cancelled. The final item in the request is marked as cancelled and cancellation details are added in the response with 'filesize' 0 "));

                    d.Append(new MarkdownHeader("Try out the get-productwithdrawn state!", 3));
                    d.Append(new MarkdownParagraph("The response mimics a situation where one of the requested products is withdrawn. The final item in the request is omitted from the returned list and is instead flagged as 'withdrawn', along with a reason like 'productWithdrawn'."));
                });
    }
}
