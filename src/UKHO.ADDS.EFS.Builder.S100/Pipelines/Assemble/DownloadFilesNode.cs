using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble
{
    internal class DownloadFilesNode : ExchangeSetPipelineNode
    {
        private readonly IFileShareReadOnlyClient _fileShareReadOnlyClient;
        private ILogger _logger;

        private const long FileSizeInBytes = 10485750;
        private const string ProductName = "ProductName";
        private const string EditionNumber = "EditionNumber";
        private const string UpdateNumber = "UpdateNumber";

        public DownloadFilesNode(IFileShareReadOnlyClient fileShareReadOnlyClient)
        {
            _fileShareReadOnlyClient = fileShareReadOnlyClient ?? throw new ArgumentNullException(nameof(fileShareReadOnlyClient));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            try
            {
                var batchDetails = context.Subject.BatchDetails;
                var downloadPath = Path.Combine(context.Subject.WorkSpaceRootPath, "fssdata");

                _logger = context.Subject.LoggerFactory.CreateLogger<DownloadFilesNode>();

                EnsureDownloadDirectoryExists(downloadPath);

                var result = await DownloadLatestBatchFilesAsync(context, SelectLatestBatchesByProductEditionAndUpdate(batchDetails));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogDownloadFilesNodeFailed(ex.Message);

                return NodeResultStatus.Failed;
            }
        }

        private static IEnumerable<BatchDetails> SelectLatestBatchesByProductEditionAndUpdate(IEnumerable<BatchDetails> batchDetails)
        {
            var latestBatches = batchDetails
                .Where(b => b.Attributes != null)
                .GroupBy(b =>
                    new
                    {
                        ProductName = b.Attributes.FirstOrDefault(a => a.Key == ProductName)?.Value,
                        EditionNumber = b.Attributes.FirstOrDefault(a => a.Key == EditionNumber)?.Value,
                        UpdateNumber = b.Attributes.FirstOrDefault(a => a.Key == UpdateNumber)?.Value
                    })
                .Select(g => g.OrderByDescending(b => b.BatchPublishedDate).First())
                .ToList();

            return latestBatches;
        }

        private async Task<NodeResultStatus> DownloadLatestBatchFilesAsync(IExecutionContext<ExchangeSetPipelineContext> context, IEnumerable<BatchDetails> latestBatches)
        {
            foreach (var batch in latestBatches)
            {
                if (!batch.Files.Any())
                {
                    continue;
                }

                foreach (var file in batch.Files)
                {
                    var fileName = file.Filename;
                    var downloadPath = Path.Combine(context.Subject.WorkSpaceRootPath, "fssdata", fileName);

                    await using var outputFileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.ReadWrite);

                    var streamResult = await _fileShareReadOnlyClient.DownloadFileAsync(
                        batch.BatchId, fileName, outputFileStream, context.Subject.Job?.CorrelationId!, FileSizeInBytes);

                    if (streamResult.IsFailure(out var error, out var value))
                    {
                        LogFssDownloadFailed(context, batch, fileName, error);

                        return NodeResultStatus.Failed;
                    }
                }
            }

            return NodeResultStatus.Succeeded;
        }

        private void LogFssDownloadFailed(IExecutionContext<ExchangeSetPipelineContext> context, BatchDetails batch, string fileName, IError error)
        {
            var downloadFilesLogView = new DownloadFilesLogView
            {
                BatchId = batch.BatchId,
                FileName = fileName,
                CorrelationId = context.Subject.Job?.CorrelationId!,
                Error = string.IsNullOrEmpty(error?.Message) ? string.Empty : error.Message
            };

            _logger.LogDownloadFilesNodeFssDownloadFailed(downloadFilesLogView);
        }

        private static void EnsureDownloadDirectoryExists(string downloadPath)
        {
            if (!Directory.Exists(downloadPath))
            {
                Directory.CreateDirectory(downloadPath);
            }
        }
    }
}
