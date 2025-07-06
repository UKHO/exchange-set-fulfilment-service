using UKHO.ADDS.EFS.Builder.S57.Pipelines.Create;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S57.Pipelines
{
    internal class CreationPipeline : IBuilderPipeline<S57ExchangeSetPipelineContext>
    {
        public async Task<NodeResult> ExecutePipeline(S57ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<S57ExchangeSetPipelineContext>();
            pipeline.AddChild(new TestCreateNode());

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
