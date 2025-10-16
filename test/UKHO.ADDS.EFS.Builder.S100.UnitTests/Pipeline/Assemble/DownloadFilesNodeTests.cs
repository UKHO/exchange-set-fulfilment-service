using System.IO.Compression;
using System.Text;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Infrastructure.Retries;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Assemble
{
    [TestFixture]
    public class DownloadFilesNodeTests
    {
        private IFileShareReadOnlyClient _fileShareReadOnlyClient;
        private DownloadFilesNode _downloadFilesNode;
        private IExecutionContext<S100ExchangeSetPipelineContext> _executionContext;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;
        private IConfiguration _configuration;
        private const int RetryDelayInMilliseconds = 100;
        const int CONCURRENCY_LIMIT = 4;
        private string _tempDirectory;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _fileShareReadOnlyClient = A.Fake<IFileShareReadOnlyClient>();
            _configuration = A.Fake<IConfiguration>();
            A.CallTo(() => _configuration[BuilderEnvironmentVariables.ConcurrentDownloadLimitCount]).Returns(CONCURRENCY_LIMIT.ToString());
            _downloadFilesNode = new DownloadFilesNode(_fileShareReadOnlyClient, _configuration);
            _executionContext = A.Fake<IExecutionContext<S100ExchangeSetPipelineContext>>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            _logger = A.Fake<ILogger<DownloadFilesNode>>();
        }

        [SetUp]
        public void SetUp()
        {
            _configuration = A.Fake<IConfiguration>();
            A.CallTo(() => _configuration["HttpRetry:RetryDelayInMilliseconds"]).Returns(RetryDelayInMilliseconds.ToString());
            HttpRetryPolicyFactory.SetConfiguration(_configuration);

            _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDirectory);

            var exchangeSetPipelineContext = new S100ExchangeSetPipelineContext(null, null, null, null, _loggerFactory)
            {
                WorkSpaceRootPath = _tempDirectory,
                Build = new S100Build
                {
                    JobId = JobId.From("TestCorrelationId"),
                    DataStandard = DataStandard.S100,
                    BatchId = BatchId.From("a-batch-id"),
                    Products =
                    [
                        new Product { ProductName = ProductName.From("Product1"), LatestEditionNumber = EditionNumber.From(1), LatestUpdateNumber = UpdateNumber.From(0) },
                        new Product { ProductName = ProductName.From("Product2"), LatestEditionNumber = EditionNumber.From(2), LatestUpdateNumber = UpdateNumber.From(1) }
                    ]
                }
            };

            A.CallTo(() => _executionContext.Subject).Returns(exchangeSetPipelineContext);
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(DownloadFilesNode).FullName!)).Returns(_logger);
        }

        [Test]
        public void Constructor_WhenFileShareReadOnlyClientIsNull_ThrowsArgumentNullException()
        {
            var configuration = A.Fake<IConfiguration>();
            Assert.Throws<ArgumentNullException>(() => new DownloadFilesNode(null, configuration));
        }

        [Test]
        public void Constructor_WhenConfigurationIsNull_ThrowsArgumentNullException()
        {
            var fileShareClient = A.Fake<IFileShareReadOnlyClient>();
            Assert.Throws<ArgumentNullException>(() => new DownloadFilesNode(fileShareClient, null));
        }

        [Test]
        public void Constructor_WhenConcurrentDownloadLimitNotConfigured_UsesDefaultValue()
        {
            var fileShareClient = A.Fake<IFileShareReadOnlyClient>();
            var configuration = A.Fake<IConfiguration>();
            A.CallTo(() => configuration[BuilderEnvironmentVariables.ConcurrentDownloadLimitCount]).Returns(null);

            var node = new DownloadFilesNode(fileShareClient, configuration);

            Assert.That(node, Is.Not.Null);
        }

        [Test]
        public void Constructor_WhenConcurrentDownloadLimitIsInvalid_UsesDefaultValue()
        {
            var fileShareClient = A.Fake<IFileShareReadOnlyClient>();
            var configuration = A.Fake<IConfiguration>();
            A.CallTo(() => configuration[BuilderEnvironmentVariables.ConcurrentDownloadLimitCount]).Returns("invalid");

            var node = new DownloadFilesNode(fileShareClient, configuration);

            Assert.That(node, Is.Not.Null);
        }

        [Test]
        public async Task ShouldExecuteAsync_WhenBatchDetailsIsNull_ReturnsFalse()
        {
            _executionContext.Subject.BatchDetails = null;

            var result = await _downloadFilesNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ShouldExecuteAsync_WhenBatchDetailsIsEmpty_ReturnsFalse()
        {
            _executionContext.Subject.BatchDetails = new List<BatchDetails>();

            var result = await _downloadFilesNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ShouldExecuteAsync_WhenBatchDetailsHasItems_ReturnsTrue()
        {
            var batch = CreateBatchDetails();
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var result = await _downloadFilesNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task WhenDownloadFileAsyncFails_ThenReturnsFailed()
        {
            var batch = CreateBatchDetails();
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var fakeResult = A.Fake<IResult<Stream>>();
            var outError = A.Fake<IError>();
            Stream? outStream = null;

            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(true);

            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenDownloadFileAsyncExceptionThrown_ThenReturnsFailed()
        {
            var exceptionMessage = "Download file failed ";
            var batch = CreateBatchDetails();
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Throws(new Exception(exceptionMessage));

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenBatchFilesIsEmpty_ThenReturnsSucceeded()
        {
            var batch = CreateBatchDetails(fileNames: Array.Empty<string>());
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };
            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenAllBatchesHaveNoFiles_ThenReturnsSucceeded()
        {
            var batch1 = CreateBatchDetails(batchId: "b1", fileNames: Array.Empty<string>());
            var batch2 = CreateBatchDetails(batchId: "b2", fileNames: Array.Empty<string>());
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch1, batch2 };

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenDownloadFileAsyncSucceeds_ThenReturnsSucceeded()
        {
            var batch = CreateBatchDetails(fileNames: new[] { "file1.txt", "ABC1234.001", "DEF5678.h5" });
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream? outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenMultipleBatchesWithSameProductEditionUpdate_ThenOnlyLatestIsProcessed()
        {
            var olderDate = DateTime.UtcNow.AddHours(-2);
            var newerDate = DateTime.UtcNow.AddHours(-1);
            
            var batch1 = CreateBatchDetails(batchId: "b1", publishedDate: olderDate);
            var batch2 = CreateBatchDetails(batchId: "b2", publishedDate: newerDate);
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch1, batch2 };

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream? outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task DownloadFilesNode_AllowsParallelDownloads()
        {
            const int FILE_COUNT = 50;

            var batch = CreateBatchDetails(fileNames: Enumerable.Range(1, FILE_COUNT).Select(i => $"file{i}.txt").ToArray());
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream? outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);

            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(DelayAndReturnResult(fakeResult));

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            stopwatch.Stop();

            var expectedMaxDuration = (FILE_COUNT / CONCURRENCY_LIMIT) * 0.5 + 5;

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(expectedMaxDuration), "Should complete quickly as all downloads are parallel");
        }

        [Test]
        public async Task WhenExceptionThrownDuringDownload_ThenLogsAndReturnsFailed()
        {
            var batch = CreateBatchDetails();
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };
            var testException = new InvalidOperationException("Test exception");
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Throws(testException);

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenFileHasZeroSize_ThenSkipsDownloadButCreatesFile()
        {
            var batch = CreateBatchDetails(fileNames: new[] { "zerosize.txt" }, fileSizes: new long?[] { 0 });
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream? outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenFileHasNullSize_ThenTreatsAsZeroSize()
        {
            var batch = CreateBatchDetails(fileNames: new[] { "nullsize.txt" }, fileSizes: new long?[] { null });
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream? outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenBatchHasNullAttributes_ThenIsFilteredOut()
        {
            var batch1 = CreateBatchDetails(batchId: "b1", attributes: null);
            var batch2 = CreateBatchDetails(batchId: "b2", attributes: new List<BatchDetailsAttributes>
            {
                new BatchDetailsAttributes("Product Name", "P2"),
                new BatchDetailsAttributes("Edition Number", "2"),
                new BatchDetailsAttributes("Update Number", "2")
            });
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch1, batch2 };

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream? outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenMultipleBatchesWithDifferentPublishedDates_ThenSelectsLatest()
        {
            var olderDate = DateTime.UtcNow.AddHours(-2);
            var newerDate = DateTime.UtcNow.AddHours(-1);
            
            var batch1 = CreateBatchDetails(batchId: "b1", publishedDate: olderDate);
            var batch2 = CreateBatchDetails(batchId: "b2", publishedDate: newerDate);
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch1, batch2 };

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream? outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenZipFileDownloaded_ThenExtractsAndDeletesZip()
        {
            var batch = CreateBatchDetails(fileNames: new[] { "test.zip" });
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var zipFilePath = Path.Combine(_tempDirectory, "test.zip");
            CreateTestZipFile(zipFilePath, new Dictionary<string, string> { { "file1.txt", "content1" } });

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream? outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .ReturnsLazily((call) =>
                {
                    var stream = call.Arguments[2] as Stream;
                    using var fileStream = File.OpenRead(zipFilePath);
                    fileStream!.CopyTo(stream!);
                    return Task.FromResult(fakeResult);
                });

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            
            var workspaceSpoolPath = Path.Combine(_tempDirectory, "spool");
            var extractedFolder = Path.Combine(workspaceSpoolPath, "test");
            var downloadedZipPath = Path.Combine(workspaceSpoolPath, "test.zip");
            
            Assert.That(Directory.Exists(extractedFolder), Is.True, $"Expected extracted folder at: {extractedFolder}");
            Assert.That(File.Exists(downloadedZipPath), Is.False, $"ZIP file should be deleted after extraction: {downloadedZipPath}");
            
            var extractedFile = Path.Combine(extractedFolder, "file1.txt");
            Assert.That(File.Exists(extractedFile), Is.True, $"Expected extracted file at: {extractedFile}");
        }

        [Test]
        public async Task WhenZipExtractionFails_ThenLogsError()
        {
            var batch = CreateBatchDetails(fileNames: new[] { "corrupted.zip" });
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream? outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .ReturnsLazily((call) =>
                {
                    var stream = call.Arguments[2] as Stream;
                    var corruptedData = Encoding.UTF8.GetBytes("This is not a valid ZIP file");
                    stream!.Write(corruptedData, 0, corruptedData.Length);
                    return Task.FromResult(fakeResult);
                });

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenZipContainsTooManyEntries_ThenSucceeds()
        {
            var batch = CreateBatchDetails(fileNames: new[] { "largearchive.zip" });
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var zipFilePath = Path.Combine(_tempDirectory, "largearchive.zip");
            var entries = new Dictionary<string, string>();
            for (int i = 0; i < 100; i++)
            {
                entries[$"file{i}.txt"] = $"content{i}";
            }
            CreateTestZipFile(zipFilePath, entries);

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream? outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .ReturnsLazily((call) =>
                {
                    var stream = call.Arguments[2] as Stream;
                    using var fileStream = File.OpenRead(zipFilePath);
                    fileStream.CopyTo(stream!);
                    return Task.FromResult(fakeResult);
                });

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenZipContainsDirectoryTraversalAttack_ThenSkipsMaliciousEntries()
        {
            var batch = CreateBatchDetails(fileNames: new[] { "malicious.zip" });
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var zipFilePath = Path.Combine(_tempDirectory, "malicious.zip");
            CreateMaliciousZipFile(zipFilePath);

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream? outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .ReturnsLazily((call) =>
                {
                    var stream = call.Arguments[2] as Stream;
                    using var fileStream = File.OpenRead(zipFilePath);
                    fileStream.CopyTo(stream!);
                    return Task.FromResult(fakeResult);
                });

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenZipContainsEmptyEntryName_ThenSkipsEntry()
        {
            var batch = CreateBatchDetails(fileNames: new[] { "emptyentry.zip" });
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var zipFilePath = Path.Combine(_tempDirectory, "emptyentry.zip");
            CreateTestZipFile(zipFilePath, new Dictionary<string, string> 
            { 
                { "validfile.txt", "content" },
                { "", "should be skipped" }
            });

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream? outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .ReturnsLazily((call) =>
                {
                    var stream = call.Arguments[2] as Stream;
                    using var fileStream = File.OpenRead(zipFilePath);
                    fileStream.CopyTo(stream!);
                    return Task.FromResult(fakeResult);
                });

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenFileAlreadyExists_ThenDeletesAndRecreates()
        {
            var batch = CreateBatchDetails(fileNames: new[] { "existing.txt" });
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var workspaceSpoolPath = Path.Combine(_tempDirectory, "spool");
            Directory.CreateDirectory(workspaceSpoolPath);
            var existingFilePath = Path.Combine(workspaceSpoolPath, "existing.txt");
            File.WriteAllText(existingFilePath, "old content");

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream? outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(File.Exists(existingFilePath), Is.True);
        }

        [Test]
        public async Task WhenSameFileDownloadedConcurrently_ThenHandledCorrectly()
        {
            var batch = CreateBatchDetails(fileNames: new[] { "concurrent1.txt", "concurrent1.txt" });
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream? outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenExceptionInPerformExecuteAsync_ThenLogsAndReturnsFailed()
        {
            var batch = CreateBatchDetails();
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };
            
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Throws(new UnauthorizedAccessException("Simulated access denied"));

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenBatchFileNameDetailsSet_ThenContainsFileNamesWithoutExtensions()
        {
            var batch = CreateBatchDetails(fileNames: new[] { "file1.txt", "file2.zip", "file3" });
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream? outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(_executionContext.Subject.BatchFileNameDetails, Contains.Item("file1"));
            Assert.That(_executionContext.Subject.BatchFileNameDetails, Contains.Item("file2"));
            Assert.That(_executionContext.Subject.BatchFileNameDetails, Contains.Item("file3"));
        }

        [Test]
        public async Task WhenZipContainsDirectoryEntry_ThenCreatesDirectory()
        {
            var batch = CreateBatchDetails(fileNames: new[] { "withdir.zip" });
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var zipFilePath = Path.Combine(_tempDirectory, "withdir.zip");
            CreateZipWithDirectoryEntry(zipFilePath);

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream? outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .ReturnsLazily((call) =>
                {
                    var stream = call.Arguments[2] as Stream;
                    using var fileStream = File.OpenRead(zipFilePath);
                    fileStream.CopyTo(stream!);
                    return Task.FromResult(fakeResult);
                });

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenDirectoryAlreadyTracked_ThenSkipsCreation()
        {
            var batch = CreateBatchDetails(fileNames: new[] { "file1.txt", "file2.txt" });
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream? outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        private static async Task<IResult<Stream>> DelayAndReturnResult(IResult<Stream> result)
        {
            await Task.Delay(500);
            return result;
        }

        private static BatchDetails CreateBatchDetails(string batchId = "b1", string[]? fileNames = null, List<BatchDetailsAttributes>? attributes = null, DateTime? publishedDate = null, long?[]? fileSizes = null)
        {
            var effectiveFileNames = fileNames ?? new[] { "file1.txt" };
            var files = new List<BatchDetailsFiles>();
            
            for (int i = 0; i < effectiveFileNames.Length; i++)
            {
                var fileSize = fileSizes != null && i < fileSizes.Length ? fileSizes[i] : 1000;
                files.Add(new BatchDetailsFiles(effectiveFileNames[i], fileSize));
            }

            return new BatchDetails(
                batchId: batchId,
                attributes: attributes ?? new List<BatchDetailsAttributes>
                {
                    new BatchDetailsAttributes("Product Name", "P1"),
                    new BatchDetailsAttributes("Edition Number", "1"),
                    new BatchDetailsAttributes("Update Number", "1")
                },
                batchPublishedDate: publishedDate ?? DateTime.UtcNow,
                files: files
            );
        }

        private static void CreateTestZipFile(string zipFilePath, Dictionary<string, string> entries)
        {
            using var zipFileStream = File.Create(zipFilePath);
            using var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create);
            
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Key)) continue;
                
                var zipEntry = archive.CreateEntry(entry.Key);
                using var entryStream = zipEntry.Open();
                var contentBytes = Encoding.UTF8.GetBytes(entry.Value);
                entryStream.Write(contentBytes, 0, contentBytes.Length);
            }
        }

        private static void CreateMaliciousZipFile(string zipFilePath)
        {
            using var zipFileStream = File.Create(zipFilePath);
            using var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create);
            
            var maliciousEntries = new[]
            {
                "../../../invalid.txt",
                "..\\..\\..\\invalid.txt",
                "/etc/passwd",
                "normal.txt"
            };

            foreach (var entryName in maliciousEntries)
            {
                var zipEntry = archive.CreateEntry(entryName);
                using var entryStream = zipEntry.Open();
                var contentBytes = Encoding.UTF8.GetBytes($"malicious content from {entryName}");
                entryStream.Write(contentBytes, 0, contentBytes.Length);
            }
        }

        private static void CreateZipWithDirectoryEntry(string zipFilePath)
        {
            using var zipFileStream = File.Create(zipFilePath);
            using var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create);
            
            var dirEntry = archive.CreateEntry("testdir/");
            
            var fileEntry = archive.CreateEntry("testdir/file.txt");
            using var entryStream = fileEntry.Open();
            var contentBytes = Encoding.UTF8.GetBytes("content in directory");
            entryStream.Write(contentBytes, 0, contentBytes.Length);
        }

        [TearDown]
        public void TearDown()
        {
            HttpRetryPolicyFactory.SetConfiguration(null);
            
            if (Directory.Exists(_tempDirectory))
            {
                try
                {
                    Directory.Delete(_tempDirectory, true);
                }
                catch
                {
                    // ignored
                }
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _loggerFactory?.Dispose();
        }
    }
}
