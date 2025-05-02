using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble
{
    public class CreateBatchNode(IFileShareReadWriteClient fileShareReadWriteClient) : ExchangeSetPipelineNode
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(
            IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var batchId = await CreateBatchAsync();
            context.Subject.BatchId = batchId;
            return NodeResultStatus.Succeeded;
        }

        private async Task<string> CreateBatchAsync()
        {
            var batchResponse = await fileShareReadWriteClient.CreateBatchAsync(new BatchModel(), "400-badrequest-guid-fss-create-batch");
            return batchResponse.IsSuccess(out var value, out _) ? value.BatchId : string.Empty;
        }
    }
}
