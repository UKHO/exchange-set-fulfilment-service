using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class DistributionPipeline : IBuilderPipeline<ExchangeSetPipelineContext>
    {
        public async Task<NodeResult> ExecutePipeline(ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<ExchangeSetPipelineContext>();

            pipeline.AddChild(new UploadFilesNode());

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
