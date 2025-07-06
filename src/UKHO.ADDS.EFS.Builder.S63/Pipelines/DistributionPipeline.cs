using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.EFS.Builder.S63.Pipelines.Distribute;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S63.Pipelines
{
    internal class DistributionPipeline : IBuilderPipeline<S63ExchangeSetPipelineContext>
    {
        private readonly IFileShareReadWriteClient _fileShareReadWriteClient;
        public DistributionPipeline(IFileShareReadWriteClient fileShareReadWriteClient)
        {
            _fileShareReadWriteClient = fileShareReadWriteClient ?? throw new ArgumentNullException(nameof(fileShareReadWriteClient));
        }

        public async Task<NodeResult> ExecutePipeline(S63ExchangeSetPipelineContext context)
        {
            var pipeline = new PipelineNode<S63ExchangeSetPipelineContext>();
            pipeline.AddChild(new TestDistributeNode());

            var result = await pipeline.ExecuteAsync(context);

            return result;
        }
    }
}
