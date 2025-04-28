using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class CreationPipeline : IBuilderPipeline<ExchangeSetPipelineContext>
    {
        public async Task<NodeResult> ExecutePipeline(ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<ExchangeSetPipelineContext>();

            pipeline.AddChild(new CreateWorkspaceNode());
            pipeline.AddChild(new CreateExchangeSetNode());

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
