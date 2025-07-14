using UKHO.ADDS.EFS.Builder.S63.Pipelines.Startup;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S63.Pipelines
{
    internal class StartupPipeline : IBuilderPipeline<S63ExchangeSetPipelineContext>
    {
        private readonly IHttpClientFactory _clientFactory;

        public StartupPipeline(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        }
        public async Task<NodeResult> ExecutePipeline(S63ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<S63ExchangeSetPipelineContext>();

            pipeline.AddChild(new ReadConfigurationNode());
            pipeline.AddChild(new GetBuildNode());
            pipeline.AddChild(new CheckEndpointsNode(_clientFactory));

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
