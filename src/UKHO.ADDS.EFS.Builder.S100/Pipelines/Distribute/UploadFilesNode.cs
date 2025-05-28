using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute
{
    internal class UploadFilesNode : ExchangeSetPipelineNode
    {
        private readonly IFileShareReadWriteClient _fileShareReadWriteClient;

        public UploadFilesNode(IFileShareReadWriteClient fileShareReadWriteClient) : base()
        {
            _fileShareReadWriteClient = fileShareReadWriteClient ?? throw new ArgumentNullException(nameof(fileShareReadWriteClient));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var logger = context.Subject.LoggerFactory.CreateLogger<ProductSearchNode>();
            var batchId = context.Subject.BatchId;
            var correlationId = context.Subject.Job.CorrelationId;

            var batchHandle = new BatchHandle(batchId);
            const string mimeType = "application/octet-stream";
            string fileName = $"S100_ExchangeSet_{DateTime.UtcNow:yyyyMMdd}.zip";

            try
            {
                var fileStream = context.Subject.ExchangeSetStream;

                var createBatchResponseResult = await _fileShareReadWriteClient.AddFileToBatchAsync(
                    batchHandle,
                    fileStream,
                    fileName,
                    mimeType,
                    correlationId,
                    CancellationToken.None
                ).ConfigureAwait(false);

                if (!createBatchResponseResult.IsSuccess(out _, out var error))
                {
                    logger.LogAddFileNodeFailed(
                        $"Failed to add file {fileName} to batch: {batchId} | CorrelationId:{correlationId} | Error: {error}");
                    return NodeResultStatus.Failed;
                }

                return NodeResultStatus.Succeeded;
            }
            catch (Exception ex)
            {
                logger.LogAddFileNodeFssAddFileFailed(
                    $"Failed to add file {fileName} to batch {batchId} | CorrelationId:{correlationId} | Error: {ex.Message}");
                return NodeResultStatus.Failed;
            }
        }
    }
}
