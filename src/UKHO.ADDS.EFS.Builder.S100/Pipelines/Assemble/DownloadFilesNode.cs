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

                context.Subject.BatchFileNameDetails = latestBatches
                    .SelectMany(b => b.Files)
                    .Select(f => Path.GetFileNameWithoutExtension(f.Filename))
                    .ToList();

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

                if (archive.Entries.Count > MaxEntryCount)
                {
                    throw new InvalidOperationException($"ZIP file contains too many entries ({archive.Entries.Count}). Maximum allowed: {MaxEntryCount}");
                }

                var totalExtractedSize = 0L;
                foreach (var entry in archive.Entries)
                {
                    if (!ShouldProcessEntry(entry))
                        continue;

                    // Generate a safe path for the entry that is guaranteed to be within the destination directory
                    var destinationPath = GetSafeDestinationPath(entry, destinationDirectoryPath);

                    // If the path is null, it means it's not safe and we should skip this entry
                    if (destinationPath == null)
                    {
                        _logger.LogWarning("Skipping potentially malicious zip entry: {EntryName}", entry.FullName);
                        continue;
                    }

                    // Now validate the entry for other security checks
                    ValidateZipEntry(entry, destinationPath, destinationDirectoryPath, ref totalExtractedSize);

                    if (IsDirectoryEntry(entry))
                    {
                        Directory.CreateDirectory(destinationPath);
                        continue;
                    }

                    // Create the parent directory for the file if it doesn't exist
                    var directoryName = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

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

        private static bool ShouldProcessEntry(ZipArchiveEntry entry)
        {
            return !string.IsNullOrEmpty(entry.FullName);
        }

        /// <summary>
        /// Generates a safe destination path for a ZIP entry, ensuring it is within the target directory.
        /// Returns null if the path would be outside the target directory (zip slip attempt).
        /// </summary>
        /// <param name="entry">The ZIP archive entry</param>
        /// <param name="destinationDirectoryPath">The base directory where files should be extracted</param>
        /// <returns>A safe path within the destination directory, or null if unsafe</returns>
        private static string? GetSafeDestinationPath(ZipArchiveEntry entry, string destinationDirectoryPath)
        {
            // Normalize the entry path to use the correct directory separator and remove any leading separators
            string normalizedEntryPath = NormalizeEntryPath(entry.FullName);

            // Construct the full destination path
            string destinationPath = Path.GetFullPath(Path.Combine(destinationDirectoryPath, normalizedEntryPath));

            // Get the full path of the destination directory with trailing separator to ensure we're comparing directories properly
            string fullDestinationDirPath = Path.GetFullPath(destinationDirectoryPath);
            if (!fullDestinationDirPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                fullDestinationDirPath += Path.DirectorySeparatorChar;
            }

            // Check if the resulting path starts with the destination directory path
            // This prevents directory traversal attacks (zip slip)
            if (!destinationPath.StartsWith(fullDestinationDirPath, StringComparison.OrdinalIgnoreCase))
            {
                // The path would be outside the target directory - possible zip slip attempt
                return null;
            }

            return destinationPath;
        }

        private static void ValidateZipEntry(ZipArchiveEntry entry, string destinationPath, string destinationDirectoryPath, ref long totalExtractedSize)
        {
            // The IsZipSlip check is now redundant as GetSafeDestinationPath already ensures path safety
            // but we'll keep it for defense in depth
            if (IsZipSlip(destinationPath, destinationDirectoryPath))
                throw new InvalidOperationException($"Entry '{entry.FullName}' is trying to extract outside of the target directory.");

            if (!IsDirectoryEntry(entry))
            {
                if (entry.Length > MaxExtractedFileSize)
                {
                    throw new InvalidOperationException($"Entry '{entry.FullName}' is too large ({entry.Length} bytes). Maximum allowed: {MaxExtractedFileSize} bytes");
                }

                if (entry.CompressedLength > 0)
                {
                    var compressionRatio = entry.Length / entry.CompressedLength;
                    if (compressionRatio > MaxCompressionRatio)
                    {
                        throw new InvalidOperationException($"Entry '{entry.FullName}' has suspicious compression ratio ({compressionRatio}). Maximum allowed: {MaxCompressionRatio}");
                    }
                }

                totalExtractedSize += entry.Length;
                if (totalExtractedSize > MaxTotalExtractedSize)
                {
                    throw new InvalidOperationException($"Total extracted size would exceed limit ({totalExtractedSize} bytes). Maximum allowed: {MaxTotalExtractedSize} bytes");
                }
            }
        }

        private static bool IsZipSlip(string destinationPath, string destinationDirectoryPath)
        {
            return !destinationPath.StartsWith(Path.GetFullPath(destinationDirectoryPath), StringComparison.OrdinalIgnoreCase);
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

        private static string NormalizeEntryPath(string entryName)
        {
            // Replace directory separators and trim leading separators
            var normalized = entryName.Replace('\\', Path.DirectorySeparatorChar)
                                      .Replace('/', Path.DirectorySeparatorChar)
                                      .TrimStart(Path.DirectorySeparatorChar);

            // Remove any parent directory traversals
            var parts = normalized.Split(Path.DirectorySeparatorChar)
                                  .Where(part => part != ".." && part != "." && !string.IsNullOrWhiteSpace(part));
            var safePath = string.Join(Path.DirectorySeparatorChar, parts);

            // Reject absolute paths
            if (Path.IsPathRooted(safePath))
            {
                return string.Empty;
            }

            return safePath;
        }

        private static bool IsDirectoryEntry(ZipArchiveEntry entry)
        {
            return entry.FullName.EndsWith("/", StringComparison.Ordinal) ||
                   entry.FullName.EndsWith("\\", StringComparison.Ordinal);
        }

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
    }
}
