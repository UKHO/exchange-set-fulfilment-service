using ESSFulfilmentService.Builder.Pipelines.Startup;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace ESSFulfilmentService.Builder.Pipelines
{
    internal class StartupPipeline : IBuilderPipeline<StartupPipelineContext>
    {
        public async Task<IResult<StartupPipelineContext>> ExecutePipeline(StartupPipelineContext context)
        {
            var pipeline = new PipelineNode<StartupPipelineContext>();

            pipeline.AddChild(new ReadConfigurationNode());
            pipeline.AddChild(new CheckEndpointsNode());
            pipeline.AddChild(new DeployWorkspaceNode());

            var result = await pipeline.ExecuteAsync(context);

            return result.Status switch
            {
                NodeResultStatus.Succeeded => Result.Success(context),
                var _ => Result.Failure<StartupPipelineContext>()
            };
        }
    }
}
