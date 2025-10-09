using System.Collections.Concurrent;
using System.IO.Compression;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.Files;
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
        private const string ProductName = "Product Name";
        private const string EditionNumber = "Edition Number";
        private const string UpdateNumber = "Update Number";
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

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<S100ExchangeSetPipelineContext> context)
        {
            return Task.FromResult(context.Subject.BatchDetails != null && context.Subject.BatchDetails.Any());
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S100ExchangeSetPipelineContext> context)
        {
            _logger = context.Subject.LoggerFactory.CreateLogger<DownloadFilesNode>();

            try
            {
                var downloadPath = Path.Combine(context.Subject.WorkSpaceRootPath, context.Subject.WorkSpaceSpoolPath);

                CreateDirectoryIfNotExists(downloadPath);

                var latestBatches = SelectLatestBatchesByProductEditionAndUpdate(context.Subject.BatchDetails);

                return await DownloadLatestBatchFilesAsync(latestBatches, downloadPath, context.Subject.Build.GetCorrelationId());
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
            CorrelationId correlationId)
        {
            var createdDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CreateDirectoryIfNotExists(workSpaceRootPath, createdDirectories);

            var allFilesToProcess = GetAllFilesToProcess(latestBatches);
            if (allFilesToProcess.Count == 0)
            {
                // Changed to return Succeeded for empty file lists
                // This allows the pipeline to continue and generate an empty exchange set
                _logger.LogDownloadFilesNodeNoFilesToProcessError("No files found for processing, continuing with empty exchange set generation");
                return NodeResultStatus.Succeeded;
            }

            // Create a shared dictionary for file locks that will be used during download
            // This dictionary is managed throughout the download process
            var fileLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

            try
            {
                var downloadTasks = CreateDownloadTasks(
                    allFilesToProcess,
                    workSpaceRootPath,
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

        private List<(BatchDetails batch, string fileName, FileSize fileSize)> GetAllFilesToProcess(IEnumerable<BatchDetails> latestBatches)
        {
            return latestBatches
                .Where(batch => batch.Files.Any())
                .SelectMany(batch => batch.Files.Select(file => (batch, file.Filename, GetFileSize(file.FileSize))))
                .ToList();
        }

        private static FileSize GetFileSize(long? nullableFileSize)
        {
            return nullableFileSize.HasValue ? FileSize.From(nullableFileSize.Value) : FileSize.Zero;
        }

        private IEnumerable<Task> CreateDownloadTasks(
            List<(BatchDetails batch, string fileName, FileSize fileSize)> allFilesToProcess,
            string workSpaceRootPath,
            CorrelationId correlationId,
            ConcurrentDictionary<string, SemaphoreSlim> fileLocks)
        {
            var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<Stream>(_logger, "DownloadFile");

            return allFilesToProcess.Select(async item =>
            {
                await _downloadFileConcurrencyLimiter.WaitAsync();
                try
                {
                    var downloadPath = Path.Combine(workSpaceRootPath, item.fileName);

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

                            if (item.fileSize != FileSize.Zero)
                            {
                                var streamResult = await retryPolicy.ExecuteAsync(() =>
                                    _fileShareReadOnlyClient.DownloadFileAsync(item.batch.BatchId, item.fileName, outputFileStream, (string)correlationId, item.fileSize.Value));

                                if (streamResult.IsFailure(out var error, out var value))
                                {
                                    LogFssDownloadFailed(item.batch, item.fileName, error);
                                    throw new Exception($"Failed to download {item.fileName}");
                                }

                                // Ensure all data is written to disk
                                await outputFileStream.FlushAsync();
                            }

                            // Close the file stream before extracting
                            await outputFileStream.DisposeAsync();

                            // Check if the file is a ZIP file and needs extraction
                            if (Path.GetExtension(downloadPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    var folderName = Path.GetFileNameWithoutExtension(downloadPath);
                                    var extractFolder = Path.Combine(workSpaceRootPath, folderName);

                                    if (!Directory.Exists(extractFolder))
                                    {
                                        Directory.CreateDirectory(extractFolder);
                                    }

                                    ExtractZipFile(downloadPath, extractFolder, item.fileName);
                                    
                                    // Delete the original ZIP file after extraction
                                    if (File.Exists(downloadPath))
                                    {
                                        File.Delete(downloadPath);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    var zipExtractionError = new ZipExtractionErrorLogView
                                    {
                                        ZipFilePath = downloadPath,
                                        DestinationDirectoryPath = Path.Combine(workSpaceRootPath, Path.GetFileNameWithoutExtension(downloadPath)),
                                        ExceptionMessage = ex.Message,
                                        ExceptionType = ex.GetType().Name,
                                        FileName = item.fileName
                                    };
                                    
                                    // Log the extraction error with structured format
                                    _logger.LogZipExtractionFailed(zipExtractionError);
                                }
                            }
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

        /// <summary>
        /// Extracts a ZIP file preserving the folder structure
        /// </summary>
        /// <param name="zipFilePath">Path to the ZIP file</param>
        /// <param name="destinationDirectoryPath">Path to extract the ZIP contents</param>
        /// <param name="originalFileName">The original file name for logging purposes</param>
        private void ExtractZipFile(string zipFilePath, string destinationDirectoryPath, string originalFileName)
        {
            try
            {
                using var archive = ZipFile.OpenRead(zipFilePath);
                
                // create all directories
                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.FullName))
                    {
                        continue;
                    }

                    var normalizedPath = entry.FullName.Replace('\\', Path.DirectorySeparatorChar)
                                                     .Replace('/', Path.DirectorySeparatorChar);
                    
                    if (normalizedPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                    {
                        var dirPath = Path.Combine(destinationDirectoryPath, normalizedPath);
                        if (!Directory.Exists(dirPath))
                        {
                            Directory.CreateDirectory(dirPath);
                        }
                    }
                    else
                    {
                        var filePath = Path.Combine(destinationDirectoryPath, normalizedPath);
                        var fileDirectory = Path.GetDirectoryName(filePath);
                        
                        if (!string.IsNullOrEmpty(fileDirectory) && !Directory.Exists(fileDirectory))
                        {
                            Directory.CreateDirectory(fileDirectory);
                        }
                    }
                }

                // extract all files
                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.FullName) || 
                        entry.FullName.EndsWith("/", StringComparison.Ordinal) || 
                        entry.FullName.EndsWith("\\", StringComparison.Ordinal))
                    {
                        continue; 
                    }

                    var normalizedPath = entry.FullName.Replace('\\', Path.DirectorySeparatorChar)
                                                     .Replace('/', Path.DirectorySeparatorChar);
                    
                    var extractPath = Path.Combine(destinationDirectoryPath, normalizedPath);
                    
                    var parentDir = Path.GetDirectoryName(extractPath);
                    if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                    {
                        Directory.CreateDirectory(parentDir);
                    }
                    
                    entry.ExtractToFile(extractPath, true);
                }
            }
            catch (Exception ex)
            {
                var zipExtractionError = new ZipExtractionErrorLogView
                {
                    ZipFilePath = zipFilePath,
                    DestinationDirectoryPath = destinationDirectoryPath,
                    ExceptionMessage = ex.Message,
                    ExceptionType = ex.GetType().Name,
                    FileName = originalFileName
                };
                
                _logger.LogZipExtractionFailed(zipExtractionError);
                
                throw;
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

        private void LogFssDownloadFailed(BatchDetails batch, string fileName, IError error)
        {
            var downloadFilesLogView = new DownloadFilesLogView
            {
                BatchId = batch.BatchId,
                FileName = fileName,
                Error = error
            };

            _logger.LogDownloadFilesNodeFssDownloadFailed(downloadFilesLogView);
        }
    }
}
