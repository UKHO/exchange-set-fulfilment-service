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
        private const string ProductName = "Product Name";
        private const string EditionNumber = "Edition Number";
        private const string UpdateNumber = "Update Number";
        private const int FileStreamBufferSize = 81920; // 80KB buffer size
        private const int DefaultConcurrentDownloads = 1; // Default number of concurrent downloads if not specified in config
        
        // Security limits for ZIP extraction
        private const long MaxExtractedFileSize = 100 * 1024 * 1024; // 100MB per file
        private const long MaxTotalExtractedSize = 500 * 1024 * 1024; // 500MB total
        private const int MaxEntryCount = 10000; // Maximum number of entries
        private const int MaxCompressionRatio = 100; // Maximum compression ratio
        private const int ExtractionBufferSize = 8192; // 8KB buffer for extraction

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
                                ExtractAndDeleteZip(downloadPath, workSpaceRootPath, item.fileName);
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

        private void ExtractAndDeleteZip(string downloadPath, string workSpaceRootPath, string originalFileName)
        {
            try
            {
                var folderName = Path.GetFileNameWithoutExtension(downloadPath);
                var extractFolder = Path.Combine(workSpaceRootPath, folderName);

                if (!Directory.Exists(extractFolder))
                {
                    Directory.CreateDirectory(extractFolder);
                }

                ExtractZipFile(downloadPath, extractFolder, originalFileName);

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
                    FileName = originalFileName
                };

                _logger.LogZipExtractionFailed(zipExtractionError);
            }
        }

        /// <summary>
        /// Securely extracts a ZIP file with resource consumption controls to prevent zip bomb attacks
        /// </summary>
        /// <param name="zipFilePath">Path to the ZIP file</param>
        /// <param name="destinationDirectoryPath">Path to extract the ZIP contents</param>
        /// <param name="originalFileName">The original file name for logging purposes</param>
        private void ExtractZipFile(string zipFilePath, string destinationDirectoryPath, string originalFileName)
        {
            try
            {
                using var archive = ZipFile.OpenRead(zipFilePath);
                
                var entryCount = 0;
                var totalExtractedSize = 0L;

                // Pre-validate entry count to prevent excessive entries
                if (archive.Entries.Count > MaxEntryCount)
                {
                    throw new InvalidOperationException($"ZIP file contains too many entries ({archive.Entries.Count}). Maximum allowed: {MaxEntryCount}");
                }

                foreach (var entry in archive.Entries)
                {
                    entryCount++;
                    
                    if (string.IsNullOrEmpty(entry.FullName))
                        continue;

                    var normalizedPath = NormalizeEntryPath(entry.FullName);
                    var destinationPath = Path.Combine(destinationDirectoryPath, normalizedPath);

                    // Zip Slip protection
                    if (!destinationPath.StartsWith(Path.GetFullPath(destinationDirectoryPath), StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"Entry '{entry.FullName}' is trying to extract outside of the target directory.");

                    if (IsDirectoryEntry(entry))
                    {
                        Directory.CreateDirectory(destinationPath);
                        continue;
                    }

                    // Validate individual file size
                    if (entry.Length > MaxExtractedFileSize)
                    {
                        throw new InvalidOperationException($"Entry '{entry.FullName}' is too large ({entry.Length} bytes). Maximum allowed: {MaxExtractedFileSize} bytes");
                    }

                    // Validate compression ratio to detect zip bombs
                    if (entry.CompressedLength > 0)
                    {
                        var compressionRatio = entry.Length / entry.CompressedLength;
                        if (compressionRatio > MaxCompressionRatio)
                        {
                            throw new InvalidOperationException($"Entry '{entry.FullName}' has suspicious compression ratio ({compressionRatio}). Maximum allowed: {MaxCompressionRatio}");
                        }
                    }

                    // Validate total extracted size
                    totalExtractedSize += entry.Length;
                    if (totalExtractedSize > MaxTotalExtractedSize)
                    {
                        throw new InvalidOperationException($"Total extracted size would exceed limit ({totalExtractedSize} bytes). Maximum allowed: {MaxTotalExtractedSize} bytes");
                    }

                    // Create directory and extract file securely
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    ExtractEntry(entry, destinationPath);
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

        /// <summary>
        /// Securely extracts a single ZIP entry with controlled resource consumption
        /// </summary>
        /// <param name="entry">The ZIP entry to extract</param>
        /// <param name="destinationPath">The destination file path</param>
        private static void ExtractEntry(ZipArchiveEntry entry, string destinationPath)
        {
            using var entryStream = entry.Open();
            using var outputStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            
            var buffer = new byte[ExtractionBufferSize];
            var totalBytesRead = 0L;
            int bytesRead;

            while ((bytesRead = entryStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                totalBytesRead += bytesRead;
                
                // Double-check against the declared length during extraction
                if (totalBytesRead > entry.Length)
                {
                    throw new InvalidOperationException($"Entry '{entry.FullName}' actual size exceeds declared size. Possible zip bomb attack.");
                }

                // Additional safety check against maximum file size
                if (totalBytesRead > MaxExtractedFileSize)
                {
                    throw new InvalidOperationException($"Entry '{entry.FullName}' size during extraction exceeds maximum allowed size ({MaxExtractedFileSize} bytes).");
                }

                outputStream.Write(buffer, 0, bytesRead);
            }
        }

        private static string NormalizeEntryPath(string entryName)
        {
            return entryName.Replace('\\', Path.DirectorySeparatorChar)
                            .Replace('/', Path.DirectorySeparatorChar)
                            .TrimStart(Path.DirectorySeparatorChar);
        }

        private static bool IsDirectoryEntry(ZipArchiveEntry entry)
        {
            return entry.FullName.EndsWith("/", StringComparison.Ordinal) ||
                   entry.FullName.EndsWith("\\", StringComparison.Ordinal);
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
