//using UKHO.ADDS.Mocks.Markdown;
//using UKHO.ADDS.Mocks.States;

//namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.fss6357
//{
//    public class GetHealthEndpoint : ServiceEndpointMock
//    {
//        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
//            endpoint.MapGet("/health", (HttpRequest request) =>
//                {
//                    var state = GetState(request);

//                    switch (state)
//                    {
//                        case WellKnownState.Default:
//                            return Results.Ok("Hello from FSS 63/57");

//                        default:
//                            // Just send default responses
//                            return WellKnownStateHandler.HandleWellKnownState(state);
//                    }
//                })
//                .WithEndpointMetadata(endpoint, d =>
//                {
//                    d.Append(new MarkdownHeader("Gets a file", 3));
//                    d.Append(new MarkdownParagraph("Try out the get-jpeg state!"));
//                });
//    }
//}

