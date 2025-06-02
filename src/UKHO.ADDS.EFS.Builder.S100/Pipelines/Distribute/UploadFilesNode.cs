using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
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

        private const string MimeType = "application/octet-stream";
        private const int FileBufferSize = 81920;
        
        public UploadFilesNode(IFileShareReadWriteClient fileShareReadWriteClient) : base()
        {
            _fileShareReadWriteClient = fileShareReadWriteClient ?? throw new ArgumentNullException(nameof(fileShareReadWriteClient));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            _logger = context.Subject.LoggerFactory.CreateLogger<UploadFilesNode>();
            var batchId = context.Subject.BatchId;
            var correlationId = context.Subject.Job.CorrelationId;
            var jobId = context.Subject.Job?.Id;

            var fileName = GetExchangeSetFileName();
            var filePath = GetExchangeSetFilePath(context.Subject.ExchangeSetFilePath,jobId);

            if (!File.Exists(filePath))
            {
                _logger.LogAddFileNodeFailed($"File not found at given path for Job Id: {jobId}");
                return NodeResultStatus.Failed;
            }

            try
            {
                await using var fileStream = OpenExchangeSetFileStream(filePath);

                var batchHandle = new BatchHandle(batchId);
                var addFileResult = await _fileShareReadWriteClient.AddFileToBatchAsync(
                    batchHandle,
                    fileStream,
                    fileName,
                    MimeType,
                    correlationId,
                    CancellationToken.None
                );

                if (!addFileResult.IsSuccess(out _, out var error))
                {
                    LogAddFileFailure(fileName, batchId, error);
                    return NodeResultStatus.Failed;
                }
                else
                {
                    context.Subject.ExchangeSetFileName = fileName;
                    return NodeResultStatus.Succeeded;
                } 
            }
            catch (Exception ex)
            {
                _logger.LogAddFileNodeFailed(ex.Message);
                return NodeResultStatus.Failed;
            }
        }

        private static string GetExchangeSetFileName() => $"S100_ExchangeSet_{DateTime.UtcNow:yyyyMMdd}.zip";

        private static string GetExchangeSetFilePath(string exchangeSetFilePath, string jobId) => Path.Combine(exchangeSetFilePath,$"{jobId}.zip");

        private static FileStream OpenExchangeSetFileStream(string filePath)
        {
            return new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                FileBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
        }

        private void LogAddFileFailure(string fileName, string batchId, IError error)
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
