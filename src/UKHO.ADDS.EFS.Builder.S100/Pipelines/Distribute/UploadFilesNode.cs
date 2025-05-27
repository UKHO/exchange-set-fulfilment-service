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
        private ILogger _logger;

        public UploadFilesNode(IFileShareReadWriteClient fileShareReadWriteClient) : base()
        {
            _fileShareReadWriteClient = fileShareReadWriteClient ?? throw new ArgumentNullException(nameof(fileShareReadWriteClient));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            _logger = context.Subject.LoggerFactory.CreateLogger<ProductSearchNode>();

            var products = context.Subject.Job?.Products;
            if (products == null || products.Count == 0)
            {
                return NodeResultStatus.NotRun;
            }

            var batchHandle = new BatchHandle(context.Subject.BatchId);
            const string filepath = "/usr/local/tomcat/ROOT/workspaces/working9/etc/JP8.zip";
            string fileName = Path.GetFileName(filepath);
            const string mimeType = "application/octet-stream";

            try
            {
                using FileStream fileStream = new(filepath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                var cancellationToken = CancellationToken.None;
                Action<(int blocksComplete, int totalBlockCount)> progressUpdate = static _ => { };

                var createBatchResponseResult = await _fileShareReadWriteClient.AddFileToBatchAsync(
                    batchHandle,
                    fileStream,
                    fileName,
                    mimeType,
                    progressUpdate,
                    context.Subject.Job.CorrelationId,
                    cancellationToken
                ).ConfigureAwait(false);

                if (!createBatchResponseResult.IsSuccess(out _, out var error))
                {
                    _logger.LogAddFileNodeFailed($"Failed to add file {fileName} to batch: {error}");
                    return NodeResultStatus.Failed;
                }

                return NodeResultStatus.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogAddFileNodeFssAddFileFailed(new AddFileLogView
                {
                    FileName = fileName,
                    BatchId = context.Subject.BatchId,
                    Error = ex.Message
                });
                return NodeResultStatus.Failed;
            }
        }
    }
}
