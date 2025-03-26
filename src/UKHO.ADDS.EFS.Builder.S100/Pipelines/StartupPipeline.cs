using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class StartupPipeline : IBuilderPipeline<PipelineContext>
    {
        public async Task<IResult<PipelineContext>> ExecutePipeline(PipelineContext context)
        {
            var pipeline = new PipelineNode<PipelineContext>();

            pipeline.AddChild(new ReadConfigurationNode());
            pipeline.AddChild(new DeployWorkspaceNode());
            pipeline.AddChild(new StartTomcatNode());
            pipeline.AddChild(new CheckEndpointsNode());

            var result = await pipeline.ExecuteAsync(context);

            return result.Status switch
            {
                NodeResultStatus.Succeeded => Result.Success(context),
                var _ => Result.Failure<PipelineContext>()
            };
        }
    }
}
