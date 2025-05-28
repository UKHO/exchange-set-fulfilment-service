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

        private const long FileSizeInBytes = 10485760;
        private const string ProductName = "ProductName";
        private const string EditionNumber = "EditionNumber";
        private const string UpdateNumber = "UpdateNumber";

        public DownloadFilesNode(IFileShareReadOnlyClient fileShareReadOnlyClient)
        {
            _fileShareReadOnlyClient = fileShareReadOnlyClient ?? throw new ArgumentNullException(nameof(fileShareReadOnlyClient));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            _logger = context.Subject.LoggerFactory.CreateLogger<DownloadFilesNode>();

            try
            {
                var downloadPath = Path.Combine(context.Subject.WorkSpaceRootPath, context.Subject.WorkSpaceSpoolPath, context.Subject.WorkSpacefssdataPath);

                EnsureDownloadDirectoryExists(downloadPath);

                var latestBatches = SelectLatestBatchesByProductEditionAndUpdate(context.Subject.BatchDetails);

                return await DownloadLatestBatchFilesAsync(latestBatches, downloadPath, context.Subject.Job.CorrelationId);
            }
            catch (Exception ex)
            {
                _logger.LogDownloadFilesNodeFailed(ex.ToString());
                return NodeResultStatus.Failed;
            }
        }

        private static IEnumerable<BatchDetails> SelectLatestBatchesByProductEditionAndUpdate(IEnumerable<BatchDetails> batchDetails)
        {
            return batchDetails
                .Where(b => b.Attributes != null)
                .Select(b =>
                {
                    var productName = b.Attributes.FirstOrDefault(a => a.Key == ProductName)?.Value;
                    var editionNumber = b.Attributes.FirstOrDefault(a => a.Key == EditionNumber)?.Value;
                    var updateNumber = b.Attributes.FirstOrDefault(a => a.Key == UpdateNumber)?.Value;
                    return new { Batch = b, productName, editionNumber, updateNumber };
                })
                .GroupBy(x => (x.productName, x.editionNumber, x.updateNumber))
                .Select(g => g.OrderByDescending(x => x.Batch.BatchPublishedDate).First().Batch);
        }

        private async Task<NodeResultStatus> DownloadLatestBatchFilesAsync(IEnumerable<BatchDetails> latestBatches, string workSpaceRootPath, string correlationId)
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

                    var downloadPath = GetDownloadPath(workSpaceRootPath, fileName);

                    await using var outputFileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.ReadWrite);

                    var streamResult = await _fileShareReadOnlyClient.DownloadFileAsync(batch.BatchId, fileName, outputFileStream, correlationId, FileSizeInBytes);

                    if (streamResult.IsFailure(out var error, out var value))
                    {
                        LogFssDownloadFailed(batch, fileName, error, correlationId);

                        return NodeResultStatus.Failed;
                    }
                }
            }

            return NodeResultStatus.Succeeded;
        }

        private void LogFssDownloadFailed(BatchDetails batch, string fileName, IError error, string correlationId)
        {
            var downloadFilesLogView = new DownloadFilesLogView
            {
                BatchId = batch.BatchId,
                FileName = fileName,
                CorrelationId = correlationId,
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

        private string GetDownloadPath(string workSpaceRootPath, string fileName)
        {
            var extension = Path.GetExtension(fileName);

            if (extension is { Length: 4 } && int.TryParse(extension[1..], out var extNum) && extNum is >= 0 and <= 999)
            {
                if (fileName.Length >= 7)
                {
                    var folderName = fileName.Substring(3, 4);
                    var folderPath = Path.Combine(workSpaceRootPath, folderName);
                    EnsureDownloadDirectoryExists(folderPath);
                    return Path.Combine(folderPath, fileName);
                }

                return Path.Combine(workSpaceRootPath, fileName);
            }

            return Path.Combine(workSpaceRootPath, fileName);
        }
    }
}
