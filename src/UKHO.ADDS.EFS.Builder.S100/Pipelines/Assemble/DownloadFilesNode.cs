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
                _logger = context.Subject.LoggerFactory.CreateLogger<DownloadFilesNode>();

                var batchDetails = context.Subject.BatchDetails;

                if (!batchDetails.Any())
                {
                    return NodeResultStatus.NotRun;
                }

                var latestBatches = GetLatestBatchDetailsList(batchDetails);

                return await DownloadLatestBatchesAsync(context, latestBatches);
            }
            catch (Exception ex)
            {
                _logger.LogDownloadFilesNodeFailed(ex.Message);
                return NodeResultStatus.Failed;
            }
        }

        private static List<BatchDetails> GetLatestBatchDetailsList(IEnumerable<BatchDetails> batchDetails)
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

        private async Task<NodeResultStatus> DownloadLatestBatchesAsync(IExecutionContext<ExchangeSetPipelineContext> context, List<BatchDetails> latestBatches)
        {
            foreach (var batch in latestBatches)
            {
                if (!batch.Files.Any())
                {
                    continue;
                }

                foreach (var file in batch.Files)
                {
                    var workSpaceRootPath = context.Subject.WorkSpaceRootPath;
                    var fileName = file.Filename;
                    var downloadPath = Path.Combine(workSpaceRootPath, fileName);

                    CheckAndCreateFolder(workSpaceRootPath);

                    var streamResult = await DownloadFileAsync(context, fileName, batch, downloadPath);

                    if (streamResult.IsFailure(out var error, out var value))
                    {
                        var downloadFilesLogView = new DownloadFilesLogView
                        {
                            BatchId = batch.BatchId,
                            FileName = fileName,
                            CorrelationId = context.Subject.Job?.CorrelationId!,
                            Error = string.IsNullOrEmpty(error?.Message) ? string.Empty : error.Message
                        };

                        _logger.LogDownloadFilesNodeFssDownloadFailed(downloadFilesLogView);
                        return NodeResultStatus.Failed;
                    }
                }
            }

            return NodeResultStatus.Succeeded;
        }

        private async Task<IResult<Stream>> DownloadFileAsync(IExecutionContext<ExchangeSetPipelineContext> context, string fileName, BatchDetails batch, string downloadPath)
        {
            await using var outputFileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.ReadWrite);

            var httpResponse = await _fileShareReadOnlyClient.DownloadFileAsync(
                batch.BatchId, fileName, outputFileStream, context.Subject.Job?.CorrelationId!, FileSizeInBytes);

            return httpResponse;
        }

        private static void CheckAndCreateFolder(string workSpaceRootPath)
        {
            if (!Directory.Exists(workSpaceRootPath))
            {
                Directory.CreateDirectory(workSpaceRootPath);
            }
        }
    }
}
