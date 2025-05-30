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
                var downloadPath = Path.Combine(context.Subject.WorkSpaceRootPath, context.Subject.WorkSpaceSpoolPath);

                CreateDirectoryIfNotExists(downloadPath);

                var latestBatches = SelectLatestBatchesByProductEditionAndUpdate(context.Subject.BatchDetails);

                return await DownloadLatestBatchFilesAsync(latestBatches, downloadPath, context.Subject.Job.CorrelationId, context.Subject.WorkSpaceSpoolDataSetFilesPath, context.Subject.WorkSpaceSpoolSupportFilesPath);
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

        private async Task<NodeResultStatus> DownloadLatestBatchFilesAsync(IEnumerable<BatchDetails> latestBatches, string workSpaceRootPath, string correlationId, string workSpaceSpoolDataSetFilesPath, string workSpaceSpoolSupportFilesPath)
        {
            // Track directories we've already created
            var createdDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Ensure main directory exists
            CreateDirectoryIfNotExists(workSpaceRootPath, createdDirectories);

            // Flatten batch and file structure into a single enumeration
            var allFilesToProcess = latestBatches
                .Where(batch => batch.Files.Any())
                .SelectMany(batch => batch.Files.Select(file => new
                {
                    Batch = batch,
                    FileName = file.Filename
                }))
                .ToList();

            // Return early if no files to process
            if (allFilesToProcess.Count == 0)
            {
                _logger.LogDownloadFilesNodeFailed($"No files to process for CorrelationId: {correlationId}");
                return NodeResultStatus.Failed;
            }

            // First, determine and create all required directories
            var fileDirectoryPaths = allFilesToProcess
                .Select(item => GetDirectoryPathForFile(workSpaceRootPath, item.FileName, workSpaceSpoolDataSetFilesPath, workSpaceSpoolSupportFilesPath))
                .Distinct();

            foreach (var directoryPath in fileDirectoryPaths)
            {
                CreateDirectoryIfNotExists(directoryPath, createdDirectories);
            }

            // Now download all files (all directories are guaranteed to exist)
            foreach (var item in allFilesToProcess)
            {
                var directoryPath = GetDirectoryPathForFile(workSpaceRootPath, item.FileName, workSpaceSpoolDataSetFilesPath, workSpaceSpoolSupportFilesPath);
                var downloadPath = Path.Combine(directoryPath, item.FileName);

                await using var outputFileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.ReadWrite);

                var streamResult = await _fileShareReadOnlyClient.DownloadFileAsync(
                    item.Batch.BatchId, item.FileName, outputFileStream, correlationId, FileSizeInBytes);

                if (streamResult.IsFailure(out var error, out var value))
                {
                    LogFssDownloadFailed(item.Batch, item.FileName, error, correlationId);
                    return NodeResultStatus.Failed;
                }
            }

            return NodeResultStatus.Succeeded;
        }

        private static void CreateDirectoryIfNotExists(string downloadPath, HashSet<string> createdDirectories = null)
        {
            // Early return if we've already created this directory
            if (createdDirectories?.Contains(downloadPath) == true)
            {
                return;
            }

            // Create directory if it doesn't exist
            if (!Directory.Exists(downloadPath))
            {
                Directory.CreateDirectory(downloadPath);
            }

            // Add to our tracking set if we're using one
            createdDirectories?.Add(downloadPath);
        }

        private string GetDirectoryPathForFile(string workSpaceRootPath, string fileName,string workSpaceSpoolDataSetFilesPath, string workSpaceSpoolSupportFilesPath)
        {
            var extension = Path.GetExtension(fileName);

            // Handle numeric extensions (.000 to .999)
            if (extension is { Length: 4 } && int.TryParse(extension[1..], out var extNum) && extNum is >= 0 and <= 999)
            {
                if (fileName.Length >= 7)
                {
                    var folderName = fileName.Substring(3, 4);
                    return Path.Combine(workSpaceRootPath, workSpaceSpoolDataSetFilesPath, folderName);
                }
            }
            // Handle .h5 extension
            else if (extension.Equals(".h5", StringComparison.OrdinalIgnoreCase))
            {
                if (fileName.Length >= 7)
                {
                    var folderName = fileName.Substring(3, 4);
                    return Path.Combine(workSpaceRootPath, workSpaceSpoolDataSetFilesPath, folderName);
                }
            }
            return Path.Combine(workSpaceRootPath, workSpaceSpoolSupportFilesPath);
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
    }
}
