using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.Mime;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.SampleService.Override.Mocks.sample
{
    public class GetFilesEndpoint : ServiceEndpointMock
    {
        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapGet("/files", (HttpRequest request) =>
                {
                    var state = GetState(request);

                    switch (state)
                    {
                        case WellKnownState.Default:
                            // ADDS Mock will have the 'default' state unless we have told it otherwise
                            return Results.Ok("This is a result, just needed this text with the 200 response");

                        case "get-file":

                            var pathResult = endpoint.GetFile("readme.txt");

                            if (pathResult.IsSuccess(out var file))
                            {
                                return Results.File(file.Path, MimeType.Text.Plain);
                            }

                            return Results.NotFound("Could not find the path in the /files GET method");

                        case "get-jpeg":

                            var jpegPathResult = endpoint.GetFile("messier-78.jpg");

                            if (jpegPathResult.IsSuccess(out var jpegFile))
                            {
                                return Results.File(jpegFile.Path, MimeType.Image.Jpeg);
                            }

                            return Results.NotFound("Could not find the JPEG path in the /files GET method");


                        default:
                            // Just send default responses
                            return WellKnownStateHandler.HandleWellKnownState(state);
                    }
                })
                .WithEndpointMetadata(endpoint, d =>
                {
                    d.Append(new MarkdownHeader("Gets a file", 3));
                    d.Append(new MarkdownParagraph("Try out the get-jpeg state!"));
                });
    }
}
