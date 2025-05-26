using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.SampleService.Override.Mocks.fss.ResponseGenerator;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.SampleService.Override.Mocks.fss
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

                    d.Append(new MarkdownParagraph("Sample query attributes:"));
                    d.Append(new MarkdownList(
                        new MarkdownTextListItem("Key $Filter"),
                        new MarkdownTextListItem("Value BusinessUnit eq 'ADDS' and $batch(ProductType) eq 's100' and  (($batch(ProductName) eq '101GB004DEVQK' and $batch(EditionNumber) eq '2' and (($batch(UpdateNumber) eq '0' or $batch(UpdateNumber) eq '1' ))))")
                    ));
                });
    }
}
