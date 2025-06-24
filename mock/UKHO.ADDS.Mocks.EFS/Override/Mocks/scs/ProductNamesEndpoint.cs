using UKHO.ADDS.Mocks.EFS.Override.Mocks.scs.ResponseGenerator;
using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.scs
{
    public class ProductNamesEndpoint : ServiceEndpointMock
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapPost("/v2/products/{productType}/ProductNames", (string productType, HttpRequest request, HttpResponse response) =>
            {
                EchoHeaders(request, response, [WellKnownHeader.CorrelationId]);

                var state = GetState(request);

                switch (state)
                {
                    case WellKnownState.Default:

                        switch (productType.ToLowerInvariant())
                        {
                            case "s100":

                                return ScsResponseGenerator.ProvideProductNamesResponse(request);

                            default:

                                return Results.BadRequest("No productType set");
                        }

                    default:

                        return WellKnownStateHandler.HandleWellKnownState(state);
                }
            })
            .Produces<string>()
            .WithEndpointMetadata(endpoint, d =>
            {
                d.Append(new MarkdownHeader("Product Names Endpoint", 3));
                d.Append(new MarkdownParagraph("This endpoint is used to retrieve product names based on the product type."));
            });
    }
}
