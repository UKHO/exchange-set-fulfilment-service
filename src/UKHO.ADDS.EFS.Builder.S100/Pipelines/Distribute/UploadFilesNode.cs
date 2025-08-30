using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging;
using UKHO.ADDS.EFS.Domain.Constants;
using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services.Factories;
using UKHO.ADDS.EFS.Domain.Services.Infrastructure.Retries;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute
{
    /// <summary>
    /// Pipeline node responsible for uploading exchange set files to the File Share Service batch.
    /// </summary>
    internal class UploadFilesNode : S100ExchangeSetPipelineNode
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
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S100ExchangeSetPipelineContext> context)
        {
            _logger = context.Subject.LoggerFactory.CreateLogger<UploadFilesNode>();
            var batchId = context.Subject.BatchId;
            var correlationId = context.Subject.Build.GetCorrelationId();
            var jobId = context.Subject.Build!.JobId;

            var fileNameGenerator = new FileNameGenerator(context.Subject.ExchangeSetNameTemplate);

            var fileName = fileNameGenerator.GenerateFileName(jobId);
            var filePath = Path.Combine(context.Subject.ExchangeSetFilePath, context.Subject.ExchangeSetArchiveFolderName, $"{jobId}.zip");

            if (!File.Exists(filePath))
            {
                LogExchangeSetFileNotFound(fileName, filePath, batchId, correlationId);
                return NodeResultStatus.Failed;
            }

            try
            {
                await using var fileStream = CreateExchangeSetFileStream(filePath);

                var batchHandle = new BatchHandle((string)batchId);
                var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<AddFileToBatchResponse>(_logger, "AddFileToBatchAsync");
                var addFileResult = await retryPolicy.ExecuteAsync(() =>
                    _fileShareReadWriteClient.AddFileToBatchAsync(
                        batchHandle,
                        fileStream,
                        fileName,
                        ApiHeaderKeys.ContentTypeOctetStream,
                        (string)correlationId,
                        CancellationToken.None
                    ));

                if (!addFileResult.IsSuccess(out _, out var error))
                {
                    LogAddFileToBatchError(fileName, batchId, error);
                    return NodeResultStatus.Failed;
                }

                return NodeResultStatus.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogUploadFilesNodeFailed(ex);
                return NodeResultStatus.Failed;
            }
        }

        /// <summary>
        /// Opens a file stream for the specified exchange set file path.
        /// </summary>
        /// <param name="filePath">The path to the exchange set file.</param>
        /// <returns>A <see cref="FileStream"/> for reading the file.</returns>
        private static FileStream CreateExchangeSetFileStream(string filePath)
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
        /// <param name="error">The error details.</param>
        private void LogAddFileToBatchError(string fileName, BatchId batchId, IError error)
        {
            var addFileLogView = new AddFileLogView
            {
                BatchId = batchId,
                FileName = fileName,
                Error = error
            };

            _logger.LogFileShareAddFileToBatchError(addFileLogView);
        }

        /// <summary>
        /// Logs information about a file that was expected but not found during an exchange set operation.
        /// </summary>
        /// <param name="fileName">The name of the file that was not found.</param>
        /// <param name="filePath">The expected file path of the missing file.</param>
        /// <param name="batchId">The identifier for the batch associated with the operation.</param>
        /// <param name="correlationId">The correlation identifier used to trace the operation across systems.</param>
        private void LogExchangeSetFileNotFound(string fileName, string filePath, BatchId batchId, CorrelationId correlationId)
        {
            var fileNotFoundLogView = new FileNotFoundLogView
            {
                FileName = fileName,
                FilePath = filePath,
                BatchId = batchId,
                CorrelationId = correlationId
            };

            _logger.LogUploadFilesNotFound(fileNotFoundLogView);
        }
    }
}
