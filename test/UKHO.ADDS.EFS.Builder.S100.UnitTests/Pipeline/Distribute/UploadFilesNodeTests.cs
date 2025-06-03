using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Distribute
{
    [TestFixture]
    internal class UploadFilesNodeTests
    {
        private IFileShareReadWriteClient _fileShareReadWriteClient;
        private UploadFilesNode _uploadFilesNode;
        private IExecutionContext<ExchangeSetPipelineContext> _executionContext;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;
        private string _tempFilePath;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _fileShareReadWriteClient = A.Fake<IFileShareReadWriteClient>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            _logger = A.Fake<ILogger<ExtractExchangeSetNode>>();

            _uploadFilesNode = new UploadFilesNode(_fileShareReadWriteClient);
            _executionContext = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
        }

        [SetUp]
        public void SetUp()
        {
            var context = new ExchangeSetPipelineContext(null, null, null, _loggerFactory)
            {
                Job = new ExchangeSetJob { CorrelationId = "TestCorrelationId", Id = "TestJobId" },
                BatchId = "TestBatchId",
                ExchangeSetFilePath = Path.GetTempPath()
            };

            A.CallTo(() => _loggerFactory.CreateLogger(typeof(UploadFilesNode).FullName!)).Returns(_logger);

            _tempFilePath = Path.Combine(context.ExchangeSetFilePath, context.Job.Id + ".zip");
            File.WriteAllText(_tempFilePath, "Temporary test file content.");
            A.CallTo(() => _executionContext.Subject).Returns(context);
        }

        [Test]
        public void WhenFileShareReadWriteClientIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new UploadFilesNode(null));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalled_ThenReturnsSucceededAndSetsBatchIdAndExchangeSetFileName()
        {
            A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(
                A<BatchHandle>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Returns(Result.Success(new AddFileToBatchResponse()));

            var result = await _uploadFilesNode.ExecuteAsync(_executionContext);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
                Assert.That(_executionContext.Subject.BatchId, Is.EqualTo("TestBatchId"));
            });
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndFileDoesNotExists_ThenReturnsFailed()
        {
            if (File.Exists(_tempFilePath))
                File.Delete(_tempFilePath);

            A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(
                A<BatchHandle>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Returns(Result.Success(new AddFileToBatchResponse()));

            var result = await _uploadFilesNode.ExecuteAsync(_executionContext);
            Assert.Multiple(() => { Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed)); });

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                LogLevel.Error,
                A<EventId>.That.Matches(e => e.Name == "AddFileNodeFailed"),
                A<LoggerMessageState>._,
                null,
                A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndAddFileToBatchFails_ThenReturnsFailed()
        {
            A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(
                A<BatchHandle>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Returns(Result.Failure<AddFileToBatchResponse>(new Error("Failed to add file to batch")));

            var result = await _uploadFilesNode.ExecuteAsync(_executionContext);

            Assert.Multiple(() => { Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed)); });
            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "AddFileNodeFssAddFileFailed"),
                    A<LoggerMessageState>._,
                    null,
                    A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndThrowsException_ThenReturnsFailed()
        {
            A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(
                A<BatchHandle>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Throws(new Exception("Test exception"));

            var result = await _uploadFilesNode.ExecuteAsync(_executionContext);

            Assert.Multiple(() => { Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed)); });
            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "AddFileNodeFailed"),
                    A<LoggerMessageState>._,
                    null,
                    A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappened();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_loggerFactory != null)
            {
                _loggerFactory.Dispose();
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_tempFilePath) && File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }
    }
}
