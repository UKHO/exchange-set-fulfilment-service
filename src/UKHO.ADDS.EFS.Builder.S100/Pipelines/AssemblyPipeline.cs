using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class AssemblyPipeline : IBuilderPipeline<S100ExchangeSetPipelineContext>
    {
        private readonly IFileShareReadOnlyClient _fileShareReadOnlyClient;

        public AssemblyPipeline(IFileShareReadOnlyClient fileShareReadOnlyClient)
        {
            _fileShareReadOnlyClient = fileShareReadOnlyClient ?? throw new ArgumentNullException(nameof(fileShareReadOnlyClient));
        }

        public async Task<NodeResult> ExecutePipeline(S100ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<S100ExchangeSetPipelineContext>();
            
            pipeline.AddChild(new ProductSearchNode(_fileShareReadOnlyClient));
            pipeline.AddChild(new DownloadFilesNode(_fileShareReadOnlyClient));

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
