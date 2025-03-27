using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class CreationPipeline : IBuilderPipeline<ExchangeSetPipelineContext>
    {
        public async Task<IResult<ExchangeSetPipelineContext>> ExecutePipeline(ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<ExchangeSetPipelineContext>();

            pipeline.AddChild(new CreateWorkspaceNode());
            pipeline.AddChild(new CreateExchangeSetNode());

            var result = await pipeline.ExecuteAsync(context);

            return result.Status switch
            {
                NodeResultStatus.Succeeded => Result.Success(context),
                var _ => Result.Failure<ExchangeSetPipelineContext>()
            };
        }
    }
}
