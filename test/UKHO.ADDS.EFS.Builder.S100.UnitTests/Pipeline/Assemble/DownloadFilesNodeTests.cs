using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
using UKHO.ADDS.EFS.Entities;
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
            var exchangeSetPipelineContext = new ExchangeSetPipelineContext(null, null, null, _loggerFactory)
            {
                WorkSpaceRootPath = Path.GetTempPath(),
                Job = new ExchangeSetJob
                {
                    CorrelationId = "TestCorrelationId",
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
            var batch = new BatchDetails(
                batchId: "b1",
                attributes:
                [
                    new BatchDetailsAttributes("ProductName", "P1"),
                    new BatchDetailsAttributes("EditionNumber", "1"),
                    new BatchDetailsAttributes("UpdateNumber", "1")
                ],
                batchPublishedDate: DateTime.UtcNow,
                files:
                [
                    new BatchDetailsFiles("file1.txt")
                ]
            );
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
            Exception loggedException = null;
            var exceptionMessage = "Download file failed ";
            var batch = new BatchDetails(
                batchId: "b1",
                attributes:
                [
                    new BatchDetailsAttributes("ProductName", "P1"),
                    new BatchDetailsAttributes("EditionNumber", "1"),
                    new BatchDetailsAttributes("UpdateNumber", "1")
                ],
                batchPublishedDate: DateTime.UtcNow,
                files:
                [
                    new BatchDetailsFiles("file1.txt")
                ]
            );
            _executionContext.Subject.BatchDetails = [batch];

            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Throws(new Exception(exceptionMessage));

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "DownloadFilesNodeFailed"),
                    A<LoggerMessageState>._,
                    A<Exception>._,
                    A<Func<LoggerMessageState, Exception?, string>>._))
                .Invokes((LogLevel level, EventId eventId, LoggerMessageState state, Exception ex, Func<LoggerMessageState, Exception?, string> formatter) =>
                {
                    loggedException = ex;
                });

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
                Assert.That(loggedException!.Message, Does.Contain(exceptionMessage));
            });
        }

        [Test]
        public async Task WhenBatchFilesIsEmpty_ThenReturnsFailed()
        {
            var batch = new BatchDetails(
                batchId: "b1",
                attributes:
                [
                    new BatchDetailsAttributes("ProductName", "P1"),
                    new BatchDetailsAttributes("EditionNumber", "1"),
                    new BatchDetailsAttributes("UpdateNumber", "1")
                ],
                batchPublishedDate: DateTime.UtcNow,
                files: []
            );
            _executionContext.Subject.BatchDetails = new List<BatchDetails> { batch };
            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }
        
        [Test]
        public async Task WhenDownloadFileAsyncSucceeds_ThenReturnsSucceeded()
        {
            var batch = new BatchDetails(
                batchId: "b1",
                attributes:
                [
                    new BatchDetailsAttributes("ProductName", "P1"),
                    new BatchDetailsAttributes("EditionNumber", "1"),
                    new BatchDetailsAttributes("UpdateNumber", "1")
                ],
                batchPublishedDate: DateTime.UtcNow,
                files:
                [
                    new BatchDetailsFiles("file1.txt"),
                    new BatchDetailsFiles("ABC1234.001"),
                    new BatchDetailsFiles("DEF5678.h5")
                ]
            );
            _executionContext.Subject.BatchDetails = [batch];

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
            var batch1 = new BatchDetails(
                batchId: "b1",
                attributes:
                [
                    new("ProductName", "P1"),
                    new BatchDetailsAttributes("EditionNumber", "1"),
                    new BatchDetailsAttributes("UpdateNumber", "1")
                ],
                batchPublishedDate: now.AddMinutes(-1),
                files:
                [
                    new BatchDetailsFiles("file1.txt")
                ]
            );
            var batch2 = new BatchDetails(
                batchId: "b2",
                attributes:
                [
                    new BatchDetailsAttributes("ProductName", "P1"),
                    new BatchDetailsAttributes("EditionNumber", "1"),
                    new BatchDetailsAttributes("UpdateNumber", "1")
                ],
                batchPublishedDate: now,
                files:
                [
                    new BatchDetailsFiles("file2.txt")
                ]
            );
            _executionContext.Subject.BatchDetails = [batch1, batch2];

            var fakeResult = A.Fake<IResult<Stream>>();
            IError outError = null;
            Stream outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);
            A.CallTo(() => _fileShareReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _downloadFilesNode.ExecuteAsync(_executionContext);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }        
    }
}
