using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class DistributionPipeline : IBuilderPipeline<ExchangeSetPipelineContext>
    {
        private readonly IFileShareReadWriteClient _fileShareReadWriteClient;
        private readonly IToolClient _toolClient;
        public DistributionPipeline(IFileShareReadWriteClient fileShareReadWriteClient, IToolClient toolClient)
        {
            _fileShareReadWriteClient = fileShareReadWriteClient ?? throw new ArgumentNullException(nameof(fileShareReadWriteClient));
            _toolClient = toolClient ?? throw new ArgumentNullException(nameof(toolClient));
        }

        public async Task<NodeResult> ExecutePipeline(ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<ExchangeSetPipelineContext>();

            pipeline.AddChild(new ExtractExchangeSetNode(_toolClient));
            pipeline.AddChild(new UploadFilesNode(_fileShareReadWriteClient));

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
