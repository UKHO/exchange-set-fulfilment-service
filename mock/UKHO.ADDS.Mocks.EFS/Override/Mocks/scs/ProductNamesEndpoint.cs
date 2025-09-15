using UKHO.ADDS.Mocks.Configuration.Mocks.scs.Generators;
using UKHO.ADDS.Mocks.EFS.Override.Mocks.scs.Constants;
using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.Configuration.Mocks.scs
{
    public class ProductNamesEndpoint : ScsEndpointBase
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
                                return await ResponseGenerator.ProvideProductNamesResponse(request);

                                default:

                                return ResponseGenerator.CreateBadRequestResponse(request, "No productType set", "Bad Request.");
                        }
                    }

                    case "get-invalidproducts":

                        response.GetTypedHeaders().LastModified = DateTime.UtcNow;
                        return await ResponseGenerator.ProvideProductNamesResponse(request, state);

                    case "get-allinvalidproducts":

                        response.GetTypedHeaders().LastModified = DateTime.UtcNow;
                        return await ResponseGenerator.ProvideProductNamesResponse(request, state);

                    case WellKnownState.BadRequest:
                        return ResponseGenerator.CreateBadRequestResponse(request, "Product Names", "Bad Request.");

                    case WellKnownState.NotFound:
                        return ResponseGenerator.CreateNotFoundResponse(request);

                    case WellKnownState.UnsupportedMediaType:
                        return ResponseGenerator.CreateUnsupportedMediaTypeResponse(ErrorResponseConstants.GenericErrorUri, "00-012-0123-01");

                    case WellKnownState.InternalServerError:
                        return ResponseGenerator.CreateInternalServerErrorResponse(request);

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
                    "")
                );
    }
}
