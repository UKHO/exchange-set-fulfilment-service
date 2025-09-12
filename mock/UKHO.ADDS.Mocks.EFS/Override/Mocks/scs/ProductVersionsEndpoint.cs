using UKHO.ADDS.Mocks.Configuration.Mocks.scs.Helpers;
using UKHO.ADDS.Mocks.Configuration.Mocks.scs.ResponseGenerator;
using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.Configuration.Mocks.scs
{
    public class ProductVersionsEndpoint : ScsEndpointBase
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapPost("/v2/products/{productType}/ProductVersions", async (string productType, HttpRequest request, HttpResponse response) =>
            {
                EchoHeaders(request, response, [WellKnownHeader.CorrelationId]);

                var contentTypeValidation = ValidateContentType(request);
                if (contentTypeValidation != null)
                {
                    return contentTypeValidation;
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
                                    return ResponseHelper.CreateBadRequestResponse(request, "No productType set", "Bad Request.");
                            }
                        }

                    case "get-invalidproducts":
                    case "get-allinvalidproducts":
                    case "get-cancelledproducts":
                    case "get-productwithdrawn":
                    case "get-productalreadytuptodate":

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
                .WithEndpointMetadata(endpoint, d => ConfigureEndpointMetadata(
                    endpoint,
                    d,
                    "Product Versions Endpoint",
                    "This endpoint is used to retrieve the latest baseline, releasable versions for requested products since a specified version.",
                    ("Try out the get-cancelledproducts state!", "The response mimics a situation where one of the requested products is cancelled. The final item in the request is marked as cancelled and cancellation details are added in the response with file size 0."),
                    ("Try out the get-productwithdrawn state!", "The response mimics a situation where one of the requested products is withdrawn. The final item in the request is omitted from the returned list and is instead flagged as 'withdrawn', along with a reason like 'productWithdrawn'."),
                    ("Try out the get-productalreadytuptodate state!", "The response mimics a situation where one of the requested products is up to date. The final item in the request is omitted from the returned list and count is shown in requestedProductsAlreadyUpToDateCount property.")
                ));
    }
}
