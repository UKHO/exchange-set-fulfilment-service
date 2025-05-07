using Microsoft.Extensions.Options;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
using UKHO.ADDS.EFS.Configuration.Builder;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using System;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class AssemblyPipeline : IBuilderPipeline<ExchangeSetPipelineContext>
    {
        private readonly IFileShareReadWriteClient _fileShareReadWriteClient;
        private readonly IFileShareReadOnlyClient _fileShareReadOnlyClient;
        private readonly IOptions<FileShareServiceSettings> _options ;

        public AssemblyPipeline(IFileShareReadOnlyClient fileShareReadOnlyClient, IFileShareReadWriteClient fileShareReadWriteClient, IOptions<FileShareServiceSettings> options)
        {
            _fileShareReadWriteClient = fileShareReadWriteClient ?? throw new ArgumentNullException(nameof(fileShareReadWriteClient));
            _fileShareReadOnlyClient = fileShareReadOnlyClient ?? throw new ArgumentNullException(nameof(fileShareReadOnlyClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<NodeResult> ExecutePipeline(ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<ExchangeSetPipelineContext>();

            pipeline.AddChild(new CreateBatchNode(_fileShareReadWriteClient));
            pipeline.AddChild(new ProductSearchNode(_fileShareReadOnlyClient, _options));
            pipeline.AddChild(new DownloadFilesNode());
            
            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
