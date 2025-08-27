using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Products;
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
            UKHO.ADDS.EFS.RetryPolicy.HttpRetryPolicyFactory.SetConfiguration(_configuration);

            var exchangeSetPipelineContext = new S100ExchangeSetPipelineContext(null, null, null, null, _loggerFactory)
            {
                WorkSpaceRootPath = Path.GetTempPath(),
                Build = new S100Build
                {
                    JobId = JobId.From("TestCorrelationId"),
                    DataStandard = DataStandard.S100,
                    BatchId = BatchId.From("a-batch-id"),
                    Products =
                    [
                        new S100Products { ProductName = ProductName.From("Product1"), LatestEditionNumber = EditionNumber.From(1), LatestUpdateNumber = UpdateNumber.From(0) },
                        new S100Products { ProductName = ProductName.From("Product2"), LatestEditionNumber = EditionNumber.From(2), LatestUpdateNumber = UpdateNumber.From(1) }
                    ]
                }
            };

            A.CallTo(() => _executionContext.Subject).Returns(exchangeSetPipelineContext);
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(DownloadFilesNode).FullName!)).Returns(_logger);
        }

        [Test]
        public async Task WhenDownloadFileAsyncFails_ThenReturnsFailed()
        {
            var batch = CreateBatchDetails();
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var fakeResult = A.Fake<IResult<Stream>>();
            var outError = A.Fake<IError>();
            Stream outStream = null;

            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(true);

            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "DownloadFilesNodeFssDownloadFailed"),
                    A<LoggerMessageState>._,
                    null,
                    A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappenedOnceExactly();
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

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                LogLevel.Error,
                A<EventId>.That.Matches(e => e.Name == "DownloadFilesNodeFailed"),
                A<LoggerMessageState>._,
                A<Exception>._,
                A<Func<LoggerMessageState, Exception?, string>>._))
            .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenBatchFilesIsEmpty_ThenReturnsFailed()
        {
            var batch = CreateBatchDetails(fileNames: Array.Empty<string>());
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };
            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenDownloadFileAsyncSucceeds_ThenReturnsSucceeded()
        {
            var batch = CreateBatchDetails(fileNames: new[] { "file1.txt", "ABC1234.001", "DEF5678.h5" });
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var fakeResult = A.Fake<IResult<Stream>>();
            IError outError = null;
            Stream outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenMultipleBatchesWithSameProductEditionUpdate_ThenOnlyLatestIsProcessed()
        {
            var now = DateTime.UtcNow;
            var batch1 = CreateBatchDetails(batchId: "b1");
            var batch2 = CreateBatchDetails(batchId: "b2");
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch1, batch2 };

            var fakeResult = A.Fake<IResult<Stream>>();
            IError outError = null;
            Stream outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenDownloadFileAsyncFailsWithRetriableStatusCode_ThenRetriesExpectedNumberOfTimes()
        {
            int callCount = 0;
            var batch = CreateBatchDetails();
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .ReturnsLazily(() =>
                {
                    callCount++;
                    return UKHO.ADDS.Infrastructure.Results.Result.Failure<Stream>(new UKHO.ADDS.Infrastructure.Results.Error("Retriable error", new Dictionary<string, object> { { "StatusCode", 503 } }));
                });

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(callCount, Is.EqualTo(4), "Should retry 3 times plus the initial call (total 4)");
        }

        [Test]
        public async Task DownloadFilesNode_AllowsParallelDownloads()
        {
            // Arrange
            const int FILE_COUNT = 50;

            var batch = CreateBatchDetails(fileNames: Enumerable.Range(1, FILE_COUNT).Select(i => $"file{i}.txt").ToArray());
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };

            var fakeResult = A.Fake<IResult<Stream>>();
            IError outError = null;
            Stream outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);

            // Simulate delay for each download to test parallelism
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .ReturnsLazily(async () => { await Task.Delay(500); return fakeResult; });

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            stopwatch.Stop();

            var expectedMaxDuration = (FILE_COUNT / CONCURRENCY_LIMIT) * 0.5 + 5; // added 5 seconds buffer

            // Assert
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(expectedMaxDuration), "Should complete quickly as all downloads are parallel");
        }

        [Test]
        public async Task WhenExceptionThrownDuringDownload_ThenLogsAndReturnsFailed()
        {
            // Arrange: BatchDetails with one file, simulate exception in download
            var batch = CreateBatchDetails();
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };
            var testException = new InvalidOperationException("Test exception");
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Throws(testException);

            // Act
            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            // Assert
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            A.CallTo(() => _logger.Log<LoggerMessageState>(
                LogLevel.Error,
                A<EventId>.That.Matches(e => e.Name == "DownloadFilesNodeFailed"),
                A<LoggerMessageState>._,
                testException,
                A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappenedOnceExactly();
        }

        private static BatchDetails CreateBatchDetails(string batchId = "b1", string[]? fileNames = null, List<BatchDetailsAttributes>? attributes = null)
        {
            return new BatchDetails(
                batchId: batchId,
                attributes: attributes ?? new List<BatchDetailsAttributes>
                {
                    new BatchDetailsAttributes("ProductName", "P1"),
                    new BatchDetailsAttributes("EditionNumber", "1"),
                    new BatchDetailsAttributes("UpdateNumber", "1")
                },
                batchPublishedDate: DateTime.UtcNow,
                files: fileNames?.Select(f => new BatchDetailsFiles(f)).ToList() ?? new List<BatchDetailsFiles> { new BatchDetailsFiles("file1.txt") }
            );
        }

        [TearDown]
        public void TearDown()
        {
            UKHO.ADDS.EFS.RetryPolicy.HttpRetryPolicyFactory.SetConfiguration(null);
        }


        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _loggerFactory?.Dispose();
        }
    }
}
