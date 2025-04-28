using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class AssemblyPipeline : IBuilderPipeline<ExchangeSetPipelineContext>
    {
        public async Task<NodeResult> ExecutePipeline(ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<ExchangeSetPipelineContext>();

            pipeline.AddChild(new QuerySalesCatalogueNode());
            pipeline.AddChild(new DownloadFilesNode());

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
