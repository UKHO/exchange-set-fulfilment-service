using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class StartupPipeline : IBuilderPipeline<ExchangeSetPipelineContext>
    {
        public async Task<IResult<ExchangeSetPipelineContext>> ExecutePipeline(ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<ExchangeSetPipelineContext>();

            pipeline.AddChild(new ReadConfigurationNode());
            pipeline.AddChild(new DeployWorkspaceNode());
            pipeline.AddChild(new StartTomcatNode());
            pipeline.AddChild(new CheckEndpointsNode());
            pipeline.AddChild(new GetJobNode());

            var result = await pipeline.ExecuteAsync(context);

            return result.Status switch
            {
                NodeResultStatus.Succeeded => Result.Success(context),
                var _ => Result.Failure<ExchangeSetPipelineContext>()
            };
        }
    }
}
