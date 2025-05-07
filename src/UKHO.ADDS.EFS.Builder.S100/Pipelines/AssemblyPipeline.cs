using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class AssemblyPipeline : IBuilderPipeline<ExchangeSetPipelineContext>
    {
        private readonly IFileShareReadWriteClient _fileShareReadWriteClient;

        public AssemblyPipeline(IFileShareReadWriteClient fileShareReadWriteClient)
        {
            _fileShareReadWriteClient = fileShareReadWriteClient ?? throw new ArgumentNullException(nameof(fileShareReadWriteClient));
        }

        public async Task<NodeResult> ExecutePipeline(ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<ExchangeSetPipelineContext>();

            pipeline.AddChild(new CreateBatchNode(_fileShareReadWriteClient));
            pipeline.AddChild(new DownloadFilesNode());

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
