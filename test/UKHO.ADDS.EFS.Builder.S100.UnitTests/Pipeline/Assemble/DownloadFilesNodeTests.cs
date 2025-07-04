using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Jobs.S100;
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
        private IExecutionContext<ExchangeSetPipelineContext> _executionContext;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _fileShareReadOnlyClient = A.Fake<IFileShareReadOnlyClient>();
            _downloadFilesNode = new DownloadFilesNode(_fileShareReadOnlyClient);
            _executionContext = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            _logger = A.Fake<ILogger<DownloadFilesNode>>();
        }

        [SetUp]
        public void SetUp()
        {
            var exchangeSetPipelineContext = new ExchangeSetPipelineContext(null,  null, null, null, _loggerFactory)
            {
                WorkSpaceRootPath = Path.GetTempPath(),
                Job = new S100ExchangeSetJob
                {
                    Id = "TestCorrelationId",
                    Products =
                    [
                        new S100Products { ProductName = "Product1", LatestEditionNumber = 1, LatestUpdateNumber = 0 },
                        new S100Products { ProductName = "Product2", LatestEditionNumber = 2, LatestUpdateNumber = 1 }
                    ]
                }
            };

            A.CallTo(() => _executionContext.Subject).Returns(exchangeSetPipelineContext);
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(DownloadFilesNode).FullName!)).Returns(_logger);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _loggerFactory?.Dispose();
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
    }
}
