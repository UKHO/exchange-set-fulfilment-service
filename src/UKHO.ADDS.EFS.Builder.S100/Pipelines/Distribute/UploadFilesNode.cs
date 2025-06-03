using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging;
using UKHO.ADDS.EFS.Constants;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute
{
    /// <summary>
    /// Pipeline node responsible for uploading exchange set files to the File Share Service batch.
    /// </summary>
    internal class UploadFilesNode : ExchangeSetPipelineNode
    {
        private readonly IFileShareReadWriteClient _fileShareReadWriteClient;
        private ILogger _logger;

        private const int FileBufferSize = 81920;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadFilesNode"/> class.
        /// </summary>
        /// <param name="fileShareReadWriteClient">The file share read/write client.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="fileShareReadWriteClient"/> is null.</exception>
        public UploadFilesNode(IFileShareReadWriteClient fileShareReadWriteClient) : base()
        {
            _fileShareReadWriteClient = fileShareReadWriteClient ?? throw new ArgumentNullException(nameof(fileShareReadWriteClient));
        }

        /// <summary>
        /// Executes the upload operation for the exchange set file.
        /// </summary>
        /// <param name="context">The execution context containing pipeline data.</param>
        /// <returns>The result status of the node execution.</returns>
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            _logger = context.Subject.LoggerFactory.CreateLogger<UploadFilesNode>();
            var batchId = context.Subject.BatchId;
            var correlationId = context.Subject.Job.CorrelationId;
            var jobId = context.Subject.Job?.Id;

            string fileName = context.Subject.ExchangeSetFileName;
            string filePath = Path.Combine(context.Subject.ExchangeSetFilePath, context.Subject.ExchangeSetOutputDirectory, $"{jobId}.zip");

            if (!File.Exists(filePath))
            {
                LogUploadFilesNotAvailable(fileName, batchId, correlationId);
                return NodeResultStatus.Failed;
            }

            try
            {
                await using var fileStream = GetExchangeSetFileStream(filePath);

                var batchHandle = new BatchHandle(batchId);
                var addFileResult = await _fileShareReadWriteClient.AddFileToBatchAsync(
                    batchHandle,
                    fileStream,
                    fileName,
                    ApiHeaderKeys.ContentTypeOctetStream,
                    correlationId,
                    CancellationToken.None
                );

                if (!addFileResult.IsSuccess(out _, out var error))
                {
                    LogAddFileFailure(fileName, batchId, correlationId, error);
                    return NodeResultStatus.Failed;
                }

                return NodeResultStatus.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogUploadFilesNodeFailed(ex.Message);
                return NodeResultStatus.Failed;
            }
        }

        /// <summary>
        /// Opens a file stream for the specified exchange set file path.
        /// </summary>
        /// <param name="filePath">The path to the exchange set file.</param>
        /// <returns>A <see cref="FileStream"/> for reading the file.</returns>
        private static FileStream GetExchangeSetFileStream(string filePath)
        {
            return new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                FileBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
        }

        /// <summary>
        /// Logs an error when adding a file to the batch fails.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="batchId">The batch identifier.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="error">The error details.</param>
        private void LogAddFileFailure(string fileName, string batchId, string correlationId, IError error)
        {
            var addFileLogView = new AddFileLogView
            {
                FileName = fileName,
                BatchId = batchId,
                CorrelationId = correlationId,
                Error = error
            };

            _logger.LogFileShareAddFileToBatchError(addFileLogView);
        }

        /// <summary>
        /// Logs a message when the upload file is not available.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="batchId">The batch identifier.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        private void LogUploadFilesNotAvailable(string fileName, string batchId, string correlationId)
        {
            var uploadFilesLogView = new UploadFilesLogView
            {
                FileName = fileName,
                BatchId = batchId,
                CorrelationId = correlationId
            };

            _logger.LogUploadFilesNotAvailable(uploadFilesLogView);
        }
    }
}
