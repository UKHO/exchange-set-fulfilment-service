using UKHO.ADDS.EFS.Builder.S63.Pipelines.Create;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S63.Pipelines
{
    internal class CreationPipeline : IBuilderPipeline<S63ExchangeSetPipelineContext>
    {
        public async Task<NodeResult> ExecutePipeline(S63ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<S63ExchangeSetPipelineContext>();
            pipeline.AddChild(new TestCreateNode());

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
