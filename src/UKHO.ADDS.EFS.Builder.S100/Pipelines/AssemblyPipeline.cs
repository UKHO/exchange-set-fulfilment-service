using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using System;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class AssemblyPipeline : IBuilderPipeline<ExchangeSetPipelineContext>
    {
        private readonly IFileShareReadWriteClient _fileShareReadWriteClient;
        private readonly IFileShareReadOnlyClient _fileShareReadOnlyClient;

        public AssemblyPipeline(IFileShareReadWriteClient fileShareReadWriteClient, IFileShareReadOnlyClient fileShareReadOnlyClient)
        {
            _fileShareReadWriteClient = fileShareReadWriteClient ?? throw new ArgumentNullException(nameof(fileShareReadWriteClient));
            _fileShareReadOnlyClient = fileShareReadOnlyClient ?? throw new ArgumentNullException(nameof(fileShareReadOnlyClient));
        }

        public async Task<NodeResult> ExecutePipeline(ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<ExchangeSetPipelineContext>();

            pipeline.AddChild(new CreateBatchNode(_fileShareReadWriteClient));
            pipeline.AddChild(new DownloadFilesNode());
            pipeline.AddChild(new ProductSearchNode(_fileShareReadOnlyClient));

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
