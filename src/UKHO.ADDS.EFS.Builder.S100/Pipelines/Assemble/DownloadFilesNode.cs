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
                var setupResult = SetupDownloadEnvironment(context);
                if (!setupResult.IsSuccess)
                {
                    return NodeResultStatus.Failed;
                }

                return await ProcessBatchDownloads(setupResult.LatestBatches, setupResult.DownloadPath, context.Subject.Build.GetCorrelationId());
            }
            catch (Exception ex)
            {
                _logger.LogDownloadFilesNodeFailed(ex);
                return NodeResultStatus.Failed;
            }
        }

        private (bool IsSuccess, IEnumerable<BatchDetails> LatestBatches, string DownloadPath) SetupDownloadEnvironment(IExecutionContext<S100ExchangeSetPipelineContext> context)
        {
            try
            {
                var downloadPath = Path.Combine(context.Subject.WorkSpaceRootPath, context.Subject.WorkSpaceSpoolPath);
                CreateDirectoryIfNotExists(downloadPath);

                var latestBatches = SelectLatestBatchesByProductEditionAndUpdate(context.Subject.BatchDetails);
                return (true, latestBatches, downloadPath);
            }
            catch
            {
                return (false, Enumerable.Empty<BatchDetails>(), string.Empty);
            }
        }

        private async Task<NodeResultStatus> ProcessBatchDownloads(IEnumerable<BatchDetails> latestBatches, string workSpaceRootPath, CorrelationId correlationId)
        {
            var createdDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CreateDirectoryIfNotExists(workSpaceRootPath, createdDirectories);

            var allFilesToProcess = GetAllFilesToProcess(latestBatches);
            if (allFilesToProcess.Count == 0)
            {
                _logger.LogDownloadFilesNodeNoFilesToProcessError("No files found for processing, continuing with empty exchange set generation");
                return NodeResultStatus.Succeeded;
            }

            return await ExecuteDownloadTasks(allFilesToProcess, workSpaceRootPath, correlationId);
        }

        private async Task<NodeResultStatus> ExecuteDownloadTasks(List<(BatchDetails batch, string fileName, FileSize fileSize)> allFilesToProcess, string workSpaceRootPath, CorrelationId correlationId)
        {
            var fileLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

            try
            {
                var downloadTasks = CreateDownloadTasks(allFilesToProcess, workSpaceRootPath, correlationId, fileLocks);
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
                CleanupFileLocks(fileLocks);
            }
        }

        private static void CleanupFileLocks(ConcurrentDictionary<string, SemaphoreSlim> fileLocks)
        {
            foreach (var semaphore in fileLocks.Values)
            {
                semaphore.Dispose();
            }
            fileLocks.Clear();
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

            return allFilesToProcess.Select(item => 
                CreateSingleDownloadTask(item, workSpaceRootPath, correlationId, fileLocks, retryPolicy));
        }

        private async Task CreateSingleDownloadTask(
            (BatchDetails batch, string fileName, FileSize fileSize) item,
            string workSpaceRootPath,
            CorrelationId correlationId,
            ConcurrentDictionary<string, SemaphoreSlim> fileLocks,
            object retryPolicy)
        {
            await _downloadFileConcurrencyLimiter.WaitAsync();
            try
            {
                await ProcessSingleFileDownload(item, workSpaceRootPath, correlationId, fileLocks, retryPolicy);
            }
            finally
            {
                _downloadFileConcurrencyLimiter.Release();
            }
        }

        private async Task ProcessSingleFileDownload(
            (BatchDetails batch, string fileName, FileSize fileSize) item,
            string workSpaceRootPath,
            CorrelationId correlationId,
            ConcurrentDictionary<string, SemaphoreSlim> fileLocks,
            object retryPolicy)
        {
            var downloadPath = Path.Combine(workSpaceRootPath, item.fileName);
            var fileLock = fileLocks.GetOrAdd(downloadPath, _ => new SemaphoreSlim(1, 1));

            await fileLock.WaitAsync();
            try
            {
                await DownloadAndProcessFile(item, downloadPath, workSpaceRootPath, correlationId, retryPolicy);
            }
            finally
            {
                fileLock.Release();
            }
        }

        private async Task DownloadAndProcessFile(
            (BatchDetails batch, string fileName, FileSize fileSize) item,
            string downloadPath,
            string workSpaceRootPath,
            CorrelationId correlationId,
            object retryPolicy)
        {
            PrepareFileForDownload(downloadPath);

            FileStream? outputFileStream = null;
            try
            {
                outputFileStream = CreateDownloadFileStream(downloadPath);
                
                if (item.fileSize != FileSize.Zero)
                {
                    await DownloadFileContent(item, outputFileStream, correlationId, retryPolicy);
                }

                await outputFileStream.DisposeAsync();
                ProcessDownloadedFile(downloadPath, workSpaceRootPath, item.fileName);
            }
            finally
            {
                if (outputFileStream != null)
                {
                    await outputFileStream.DisposeAsync();
                }
            }
        }

        private static void PrepareFileForDownload(string downloadPath)
        {
            if (File.Exists(downloadPath))
            {
                File.Delete(downloadPath);
            }
        }

        private static FileStream CreateDownloadFileStream(string downloadPath)
        {
            return new FileStream(
                downloadPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                FileStreamBufferSize,
                FileOptions.Asynchronous);
        }

        private async Task DownloadFileContent(
            (BatchDetails batch, string fileName, FileSize fileSize) item,
            FileStream outputFileStream,
            CorrelationId correlationId,
            object retryPolicy)
        {
            // Use dynamic to avoid complex type casting - the retry policy ExecuteAsync method is generic
            dynamic dynamicRetryPolicy = retryPolicy;
            
            Func<Task<IResult<Stream>>> downloadFunc = () =>
                _fileShareReadOnlyClient.DownloadFileAsync(item.batch.BatchId, item.fileName, outputFileStream, (string)correlationId, item.fileSize.Value);
            
            IResult<Stream> streamResult = await dynamicRetryPolicy.ExecuteAsync(downloadFunc);

            if (streamResult.IsFailure(out IError error, out Stream value))
            {
                LogFssDownloadFailed(item.batch, item.fileName, error);
                throw new Exception($"Failed to download {item.fileName}");
            }

            await outputFileStream.FlushAsync();
        }

        private void ProcessDownloadedFile(string downloadPath, string workSpaceRootPath, string fileName)
        {
            if (Path.GetExtension(downloadPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                ExtractAndDeleteZip(downloadPath, workSpaceRootPath, fileName);
            }
        }

        private void ExtractAndDeleteZip(string downloadPath, string workSpaceRootPath, string originalFileName)
        {
            try
            {
                var extractionResult = PrepareZipExtraction(downloadPath, workSpaceRootPath);
                if (!extractionResult.IsSuccess)
                {
                    return;
                }

                ExtractZipFile(downloadPath, extractionResult.ExtractFolder, originalFileName);
                
                if (File.Exists(downloadPath))
                {
                    File.Delete(downloadPath);
                }
            }
            catch (Exception ex)
            {
                LogZipExtractionError(downloadPath, workSpaceRootPath, originalFileName, ex);
            }
        }

        private static (bool IsSuccess, string ExtractFolder) PrepareZipExtraction(string downloadPath, string workSpaceRootPath)
        {
            try
            {
                var folderName = Path.GetFileNameWithoutExtension(downloadPath);
                var extractFolder = Path.Combine(workSpaceRootPath, folderName);

                if (!Directory.Exists(extractFolder))
                {
                    Directory.CreateDirectory(extractFolder);
                }

                return (true, extractFolder);
            }
            catch
            {
                return (false, string.Empty);
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
                
                ValidateZipArchive(archive);
                var extractionState = new ZipExtractionState();
                
                foreach (var entry in archive.Entries)
                {
                    ProcessZipEntry(entry, destinationDirectoryPath, extractionState);
                }
            }
            catch (Exception ex)
            {
                LogZipExtractionError(zipFilePath, destinationDirectoryPath, originalFileName, ex);
                throw;
            }
        }

        private static void ValidateZipArchive(ZipArchive archive)
        {
            if (archive.Entries.Count > MaxEntryCount)
            {
                throw new InvalidOperationException($"ZIP file contains too many entries ({archive.Entries.Count}). Maximum allowed: {MaxEntryCount}");
            }
        }

        private static void ProcessZipEntry(ZipArchiveEntry entry, string destinationDirectoryPath, ZipExtractionState state)
        {
            if (string.IsNullOrEmpty(entry.FullName))
                return;

            var fileName = Path.GetFileName(entry.FullName.Replace("../", "").Replace("..\\", ""));
            if (string.IsNullOrEmpty(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return;

            var destinationPath = Path.Combine(destinationDirectoryPath, fileName);

            ValidateZipSlipProtection(entry.FullName, destinationPath, destinationDirectoryPath);

            if (IsDirectoryEntry(entry))
            {
                Directory.CreateDirectory(destinationPath);
                return;
            }

            ValidateZipEntry(entry, state);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            ExtractEntry(entry, destinationPath);
        }

        private static void ValidateZipSlipProtection(string entryFullName, string destinationPath, string destinationDirectoryPath)
        {
            if (!destinationPath.StartsWith(Path.GetFullPath(destinationDirectoryPath), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Entry '{entryFullName}' is trying to extract outside of the target directory.");
        }

        private static void ValidateZipEntry(ZipArchiveEntry entry, ZipExtractionState state)
        {
            if (entry.Length > MaxExtractedFileSize)
            {
                throw new InvalidOperationException($"Entry '{entry.FullName}' is too large ({entry.Length} bytes). Maximum allowed: {MaxExtractedFileSize} bytes");
            }

            ValidateCompressionRatio(entry);
            
            state.TotalExtractedSize += entry.Length;
            if (state.TotalExtractedSize > MaxTotalExtractedSize)
            {
                throw new InvalidOperationException($"Total extracted size would exceed limit ({state.TotalExtractedSize} bytes). Maximum allowed: {MaxTotalExtractedSize} bytes");
            }
        }

        private static void ValidateCompressionRatio(ZipArchiveEntry entry)
        {
            if (entry.CompressedLength > 0)
            {
                var compressionRatio = entry.Length / entry.CompressedLength;
                if (compressionRatio > MaxCompressionRatio)
                {
                    throw new InvalidOperationException($"Entry '{entry.FullName}' has suspicious compression ratio ({compressionRatio}). Maximum allowed: {MaxCompressionRatio}");
                }
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
                
                ValidateExtractedBytes(entry, totalBytesRead);
                outputStream.Write(buffer, 0, bytesRead);
            }
        }

        private static void ValidateExtractedBytes(ZipArchiveEntry entry, long totalBytesRead)
        {
            if (totalBytesRead > entry.Length)
            {
                throw new InvalidOperationException($"Entry '{entry.FullName}' actual size exceeds declared size. Possible zip bomb attack.");
            }

            if (totalBytesRead > MaxExtractedFileSize)
            {
                throw new InvalidOperationException($"Entry '{entry.FullName}' size during extraction exceeds maximum allowed size ({MaxExtractedFileSize} bytes).");
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

        private void LogZipExtractionError(string zipFilePath, string destinationDirectoryPath, string originalFileName, Exception ex)
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
        }

        private class ZipExtractionState
        {
            public long TotalExtractedSize { get; set; }
        }
    }
}
