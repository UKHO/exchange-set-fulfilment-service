using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class StartupPipeline : IBuilderPipeline<ExchangeSetPipelineContext>
    {
        public async Task<NodeResult> ExecutePipeline(ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<ExchangeSetPipelineContext>();

            pipeline.AddChild(new ReadConfigurationNode());
            pipeline.AddChild(new StartTomcatNode());   
            pipeline.AddChild(new CheckEndpointsNode());
            pipeline.AddChild(new GetJobNode());

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
