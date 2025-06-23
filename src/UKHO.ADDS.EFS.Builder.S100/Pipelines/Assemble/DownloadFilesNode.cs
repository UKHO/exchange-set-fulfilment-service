using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.EFS.RetryPolicy;
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
        private const int ProducerCodeStartIndex = 3;
        private const int ProducerCodeLength = 4;
        private const string H5Extension = ".h5";
        private const int MinimumFilenameLength = 7; // Producer code starts at index 3 and is 4 chars long
        private const int NumericExtensionLength = 4; // Includes the period (.)
        private const int NumericPartStartIndex = 1;  // Skip the period
        private const int MinNumericValue = 000;
        private const int MaxNumericValue = 999;

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
                _logger.LogDownloadFilesNodeFailed(ex);
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
                _logger.LogDownloadFilesNodeNoFilesToProcessError("No files found for processing");
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
            var semaphore = new SemaphoreSlim(4); // Limit to 4 concurrent downloads (tune as needed)
            var downloadTasks = allFilesToProcess.Select(async item =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var directoryPath = GetDirectoryPathForFile(workSpaceRootPath, item.FileName, workSpaceSpoolDataSetFilesPath, workSpaceSpoolSupportFilesPath);
                    var downloadPath = Path.Combine(directoryPath, item.FileName);

                    await using var outputFileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write);

                    var streamResult = await _fileShareReadOnlyClient.DownloadFileAsync(
                        item.Batch.BatchId, item.FileName, outputFileStream, correlationId, FileSizeInBytes);

                    if (streamResult.IsFailure(out var error, out var value))
                    {
                        LogFssDownloadFailed(item.Batch, item.FileName, error, correlationId);
                        throw new Exception($"Failed to download {item.FileName}");
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            try
            {
                await Task.WhenAll(downloadTasks);
                return NodeResultStatus.Succeeded;
            }
            catch
            {
                return NodeResultStatus.Failed;
            }
        }

        private static void CreateDirectoryIfNotExists(string downloadPath, HashSet<string>? createdDirectories = null)
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

        private string GetDirectoryPathForFile(string workSpaceRootPath, string fileName, string workSpaceSpoolDataSetFilesPath, string workSpaceSpoolSupportFilesPath)
        {
            var extension = Path.GetExtension(fileName);

            // Check if file should go into dataset-specific folder
            if (IsDatasetFile(fileName, extension))
            {
                // Extract the producer code from filename (chars 4-7 per S-100 standard)
                var producerCode = fileName.Substring(ProducerCodeStartIndex, ProducerCodeLength);
                return Path.Combine(workSpaceRootPath, workSpaceSpoolDataSetFilesPath, producerCode);
            }

            // All other files go to support files folder
            return Path.Combine(workSpaceRootPath, workSpaceSpoolSupportFilesPath);
        }

        /// <summary>
        /// Determines if the file is an S-100 dataset file that should be placed in a producer-specific subfolder.
        /// </summary>
        /// <param name="fileName">The name of the file</param>
        /// <param name="extension">The file extension</param>
        /// <returns>True if the file is a dataset file, false otherwise</returns>
        private static bool IsDatasetFile(string fileName, string extension)
        {
            // File must be long enough to contain a producer code
            if (fileName.Length < MinimumFilenameLength)
            {
                return false;
            }

            // Check for numeric extensions (.000 to .999)
            var isNumericExtension = extension is { Length: NumericExtensionLength } &&
                                     int.TryParse(extension[NumericPartStartIndex..], out var extNum) &&
                                     extNum is >= MinNumericValue and <= MaxNumericValue;

            // Either numeric extension or H5 extension
            return isNumericExtension || extension.Equals(H5Extension, StringComparison.OrdinalIgnoreCase);
        }

        private void LogFssDownloadFailed(BatchDetails batch, string fileName, IError error, string correlationId)
        {
            var downloadFilesLogView = new DownloadFilesLogView
            {
                BatchId = batch.BatchId,
                FileName = fileName,
                CorrelationId = correlationId,
                Error = error
            };

            _logger.LogDownloadFilesNodeFssDownloadFailed(downloadFilesLogView);
        }
    }
}
