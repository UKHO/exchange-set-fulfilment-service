using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class CreationPipeline : IBuilderPipeline<S100ExchangeSetPipelineContext>
    {
        public async Task<NodeResult> ExecutePipeline(S100ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<S100ExchangeSetPipelineContext>();

            pipeline.AddChild(new AddExchangeSetNode());
            pipeline.AddChild(new AddContentExchangeSetNode());
            pipeline.AddChild(new SignExchangeSetNode());

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
