using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble
{
    internal class CreateBatchNode : ExchangeSetPipelineNode
    {
        private readonly IFileShareReadWriteClient _fileShareReadWriteClient;
        public CreateBatchNode(IFileShareReadWriteClient fileShareReadWriteClient)
        {
            _fileShareReadWriteClient = fileShareReadWriteClient;
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(
            IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var batchId = await CreateBatchAsync();
            context.Subject.BatchId = batchId;
            return NodeResultStatus.Succeeded;
        }

        private async Task<string> CreateBatchAsync()
        {
            var batchResponse = await _fileShareReadWriteClient.CreateBatchAsync(new BatchModel(), "Test102");
            if (batchResponse.IsSuccess(out var value, out _))
            {
                return value.BatchId ?? string.Empty;
            }
            return string.Empty;
        }
    }
}
