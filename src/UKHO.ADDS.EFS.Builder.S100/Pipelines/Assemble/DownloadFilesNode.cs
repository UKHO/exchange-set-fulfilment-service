using System.Collections.Concurrent;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Infrastructure.Retries;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble
{
    internal class DownloadFilesNode : S100ExchangeSetPipelineNode
    {
        private readonly IFileShareReadOnlyClient _fileShareReadOnlyClient;
        private readonly IConfiguration _configuration;
        private readonly int _maxConcurrentDownloads;
        private readonly SemaphoreSlim _downloadFileConcurrencyLimiter;
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
        private const int FileStreamBufferSize = 81920; // 80KB buffer size
        private const int DefaultConcurrentDownloads = 1; // Default number of concurrent downloads if not specified in config

        public DownloadFilesNode(IFileShareReadOnlyClient fileShareReadOnlyClient, IConfiguration configuration)
        {
            _fileShareReadOnlyClient = fileShareReadOnlyClient ?? throw new ArgumentNullException(nameof(fileShareReadOnlyClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            // First try to get the configuration from the UKHO.ADDS.EFS.Builder.S100 section
            if (!int.TryParse(_configuration[BuilderEnvironmentVariables.ConcurrentDownloadLimitCount], out _maxConcurrentDownloads))
            {
                // Fall back to default value if not configured
                _maxConcurrentDownloads = DefaultConcurrentDownloads;
            }
            
            _downloadFileConcurrencyLimiter = new SemaphoreSlim(_maxConcurrentDownloads);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S100ExchangeSetPipelineContext> context)
        {
            _logger = context.Subject.LoggerFactory.CreateLogger<DownloadFilesNode>();

            try
            {
                var downloadPath = Path.Combine(context.Subject.WorkSpaceRootPath, context.Subject.WorkSpaceSpoolPath);

                CreateDirectoryIfNotExists(downloadPath);

                var latestBatches = SelectLatestBatchesByProductEditionAndUpdate(context.Subject.BatchDetails);

                return await DownloadLatestBatchFilesAsync(latestBatches, downloadPath, context.Subject.Build.GetCorrelationId(), context.Subject.WorkSpaceSpoolDataSetFilesPath, context.Subject.WorkSpaceSpoolSupportFilesPath);
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

        private async Task<NodeResultStatus> DownloadLatestBatchFilesAsync(
            IEnumerable<BatchDetails> latestBatches,
            string workSpaceRootPath,
            CorrelationId correlationId,
            string workSpaceSpoolDataSetFilesPath,
            string workSpaceSpoolSupportFilesPath)
        {
            var createdDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CreateDirectoryIfNotExists(workSpaceRootPath, createdDirectories);

            var allFilesToProcess = GetAllFilesToProcess(latestBatches);
            if (allFilesToProcess.Count == 0)
            {
                _logger.LogDownloadFilesNodeNoFilesToProcessError("No files found for processing");
                return NodeResultStatus.Failed;
            }

            CreateRequiredDirectories(
                allFilesToProcess,
                workSpaceRootPath,
                workSpaceSpoolDataSetFilesPath,
                workSpaceSpoolSupportFilesPath,
                createdDirectories);

            // Create a shared dictionary for file locks that will be used during download
            // This dictionary is managed throughout the download process
            var fileLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
            
            try
            {
                var downloadTasks = CreateDownloadTasks(
                    allFilesToProcess,
                    workSpaceRootPath,
                    workSpaceSpoolDataSetFilesPath,
                    workSpaceSpoolSupportFilesPath,
                    correlationId,
                    fileLocks);

                await Task.WhenAll(downloadTasks);
                return NodeResultStatus.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogDownloadFilesNodeFailed(ex);
                return NodeResultStatus.Failed;
            }
            finally
            {
                // Clean up all semaphores AFTER all tasks have completed
                foreach (var semaphore in fileLocks.Values)
                {
                    semaphore.Dispose();
                }
                fileLocks.Clear();
            }
        }

        private List<(BatchDetails Batch, string FileName)> GetAllFilesToProcess(IEnumerable<BatchDetails> latestBatches)
        {
            return latestBatches
                .Where(batch => batch.Files.Any())
                .SelectMany(batch => batch.Files.Select(file => (batch, file.Filename)))
                .ToList();
        }

        private void CreateRequiredDirectories(
            List<(BatchDetails Batch, string FileName)> allFilesToProcess,
            string workSpaceRootPath,
            string workSpaceSpoolDataSetFilesPath,
            string workSpaceSpoolSupportFilesPath,
            HashSet<string> createdDirectories)
        {
            var fileDirectoryPaths = allFilesToProcess
                .Select(item => GetDirectoryPathForFile(
                    workSpaceRootPath,
                    item.FileName,
                    workSpaceSpoolDataSetFilesPath,
                    workSpaceSpoolSupportFilesPath))
                .Distinct();

            foreach (var directoryPath in fileDirectoryPaths)
            {
                CreateDirectoryIfNotExists(directoryPath, createdDirectories);
            }
        }

        private IEnumerable<Task> CreateDownloadTasks(
            List<(BatchDetails Batch, string FileName)> allFilesToProcess,
            string workSpaceRootPath,
            string workSpaceSpoolDataSetFilesPath,
            string workSpaceSpoolSupportFilesPath,
            CorrelationId correlationId,
            ConcurrentDictionary<string, SemaphoreSlim> fileLocks)
        {
            var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<Stream>(_logger, "DownloadFile");

            return allFilesToProcess.Select(async item =>
            {
                await _downloadFileConcurrencyLimiter.WaitAsync();
                try
                {
                    var directoryPath = GetDirectoryPathForFile(
                        workSpaceRootPath,
                        item.FileName,
                        workSpaceSpoolDataSetFilesPath,
                        workSpaceSpoolSupportFilesPath);
                    var downloadPath = Path.Combine(directoryPath, item.FileName);

                    // Get or create a semaphore for this specific file to prevent concurrent access
                    var fileLock = fileLocks.GetOrAdd(downloadPath, _ => new SemaphoreSlim(1, 1));

                    await fileLock.WaitAsync();
                    try
                    {
                        // Check if file already exists and delete it to ensure clean state
                        if (File.Exists(downloadPath))
                        {
                            File.Delete(downloadPath);
                        }

                        FileStream? outputFileStream = null;
                        try
                        {
                            outputFileStream = new FileStream(
                                downloadPath,
                                FileMode.Create,
                                FileAccess.Write,
                                FileShare.None,
                                FileStreamBufferSize,
                                FileOptions.Asynchronous);

                            var streamResult = await retryPolicy.ExecuteAsync(() =>
                                _fileShareReadOnlyClient.DownloadFileAsync(item.Batch.BatchId, item.FileName, outputFileStream, (string)correlationId, FileSizeInBytes));

                            if (streamResult.IsFailure(out var error, out var value))
                            {
                                LogFssDownloadFailed(item.Batch, item.FileName, error, correlationId);
                                throw new Exception($"Failed to download {item.FileName}");
                            }

                            // Ensure all data is written to disk
                            await outputFileStream.FlushAsync();
                        }
                        finally
                        {
                            // Dispose the file stream in finally block to ensure it's always closed
                            if (outputFileStream != null)
                            {
                                await outputFileStream.DisposeAsync();
                            }
                        }
                    }
                    finally
                    {
                        fileLock.Release();
                        // DO NOT dispose the semaphore here - it will be disposed collectively at the end
                    }
                }
                finally
                {
                    _downloadFileConcurrencyLimiter.Release();
                }
            });
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

            // Check for numeric extensions (.000 to .999) using ReadOnlySpan for better performance
            if (extension.Length == NumericExtensionLength)
            {
                ReadOnlySpan<char> numericPart = extension.AsSpan(NumericPartStartIndex);
                if (int.TryParse(numericPart, out var extNum) &&
                    extNum is >= MinNumericValue and <= MaxNumericValue)
                {
                    return true;
                }
            }

            // H5 extension
            return extension.Equals(H5Extension, StringComparison.OrdinalIgnoreCase);
        }

        private void LogFssDownloadFailed(BatchDetails batch, string fileName, IError error, CorrelationId correlationId)
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
