using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute
{
    internal class UploadFilesNode : ExchangeSetPipelineNode
    {
        private readonly IFileShareReadWriteClient _fileShareReadWriteClient;
        private ILogger _logger;

        public UploadFilesNode(IFileShareReadWriteClient fileShareReadWriteClient) : base()
        {
            _fileShareReadWriteClient = fileShareReadWriteClient ?? throw new ArgumentNullException(nameof(fileShareReadWriteClient));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            _logger = context.Subject.LoggerFactory.CreateLogger<UploadFilesNode>();
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
                );

                if (!createBatchResponseResult.IsSuccess(out _, out var error))
                {
                    CreateAddFileLogView(fileName, context.Subject.BatchId, error);
                    return NodeResultStatus.Failed;
                }

                return NodeResultStatus.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogAddFileNodeFailed(ex.Message);
                return NodeResultStatus.Failed;
            }
        }

        private void CreateAddFileLogView(string fileName, string batchId, IError error)
        {
            var addFileLogView = new AddFileLogView
            {
                FileName = fileName,
                BatchId = batchId,
                Error = error
            };

            _logger.LogAddFileNodeFssAddFileFailed(addFileLogView);
        }

    }
}
