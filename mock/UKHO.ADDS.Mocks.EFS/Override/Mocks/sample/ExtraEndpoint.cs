using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.SampleService.Override.Mocks.sample.Models;

namespace UKHO.ADDS.Mocks.SampleService.Override.Mocks.sample
{
    public class ExtraEndpoint : ServiceEndpointMock
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapPut("/extra", (HttpRequest request, MockFileModel model) => { return Results.Ok("Here is my extra endpoint"); }).WithEndpointMetadata(endpoint, d => { d.Append(new MarkdownParagraph(new MarkdownStrongEmphasis("Don't ever call this endpoint"))); });
    }
}
