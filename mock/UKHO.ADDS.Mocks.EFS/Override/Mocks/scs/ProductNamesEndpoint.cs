using UKHO.ADDS.Mocks.Configuration.Mocks.scs.Helpers;
using UKHO.ADDS.Mocks.Configuration.Mocks.scs.ResponseGenerator;
using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.Configuration.Mocks.scs
{
    public class ProductNamesEndpoint : ServiceEndpointMock
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapPost("/v2/products/{productType}/ProductNames", async (string productType, HttpRequest request, HttpResponse response) =>
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

                                response.GetTypedHeaders().LastModified = DateTime.UtcNow;
                                return await ScsResponseGenerator.ProvideProductNamesResponse(request);

                            default:

                                return ResponseHelper.CreateBadRequestResponse(request, "No productType set", "Bad Request.");
                        }
                    }

                    case "get-invalidproducts":

                        response.GetTypedHeaders().LastModified = DateTime.UtcNow;
                        return await ScsResponseGenerator.ProvideProductNamesResponse(request, state);

                    case "get-allinvalidproducts":

                        response.GetTypedHeaders().LastModified = DateTime.UtcNow;
                        return await ScsResponseGenerator.ProvideProductNamesResponse(request, state);

                    case WellKnownState.BadRequest:
                        return ResponseHelper.CreateBadRequestResponse(request, "Product Names", "Bad Request.");

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
                    d.Append(new MarkdownHeader("Product Names Endpoint", 3));
                    d.Append(new MarkdownParagraph("This endpoint is used to retrieve product names based on the product type."));

                    d.Append(new MarkdownHeader("Try out the get-invalidproducts state!", 3));
                    d.Append(new MarkdownParagraph("The response mimics a situation where one of the requested products is unavailable. The final item in the request is omitted from the returned list and is instead flagged as 'not returned', along with a reason like 'invalidProduct'."));

                    d.Append(new MarkdownHeader("Try out the get-allinvalidproducts state!", 3));
                    d.Append(new MarkdownParagraph("The response mimics a situation where ALL requested products are invalid. No products are returned and all requested products are flagged as 'not returned' with reason 'invalidProduct'. This simulates the scenario where an error should be logged and no exchange set should be created."));
                });
    }
}
