using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class StartupPipeline : IBuilderPipeline<S100ExchangeSetPipelineContext>
    {
        private readonly IHttpClientFactory _clientFactory;

        public StartupPipeline(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        }
        public async Task<NodeResult> ExecutePipeline(S100ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<S100ExchangeSetPipelineContext>();

            pipeline.AddChild(new ReadConfigurationNode());
            pipeline.AddChild(new StartTomcatNode());
            pipeline.AddChild(new CheckEndpointsNode(_clientFactory));
            pipeline.AddChild(new GetJobNode());

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
