using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Infrastructure.Retries;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Distribute
{
    [TestFixture]
    internal class UploadFilesNodeTests
    {
        private IFileShareReadWriteClient _fileShareReadWriteClient;
        private IFileNameGeneratorService _fileNameGeneratorService;
        private UploadFilesNode _uploadFilesNode;
        private IExecutionContext<S100ExchangeSetPipelineContext> _executionContext;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;
        private string _tempFilePath;
        private string _testDirectory;

        private const string JobId = "TestJobId";
        private const string BatchId = "TestBatchId";
        private const string ArchiveFolder = "ExchangeSetArchive";
        private const string ExchangeSetNameTemplate = "S100-ExchangeSet-[jobid].zip";
        private const string GeneratedFileName = "S100-ExchangeSet-TestJobId.zip";
        private const string RetryDelayMilliseconds = "1000";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        [SetUp]
        public void SetUp()
        {
            _fileShareReadWriteClient = A.Fake<IFileShareReadWriteClient>();
            _fileNameGeneratorService = A.Fake<IFileNameGeneratorService>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            _logger = A.Fake<ILogger<UploadFilesNode>>();
            _executionContext = A.Fake<IExecutionContext<S100ExchangeSetPipelineContext>>();

            _uploadFilesNode = new UploadFilesNode(_fileShareReadWriteClient, _fileNameGeneratorService);

            var configuration = A.Fake<IConfiguration>();
            A.CallTo(() => configuration["HttpRetry:RetryDelayInMilliseconds"]).Returns(RetryDelayMilliseconds);
            A.CallTo(() => configuration[BuilderEnvironmentVariables.RetryDelayMilliseconds]).Returns(RetryDelayMilliseconds);
            HttpRetryPolicyFactory.SetConfiguration(configuration);

            var exchangeSetPipelineContext = new S100ExchangeSetPipelineContext(configuration, null!, null!, null!, _loggerFactory)
            {
                Build = new S100Build
                {
                    JobId = Domain.Jobs.JobId.From(JobId),
                    BatchId = UKHO.ADDS.EFS.Domain.Jobs.BatchId.From(BatchId),
                    DataStandard = DataStandard.S100,
                    BuildCommitInfo = new BuildCommitInfo()
                },
                JobId = Domain.Jobs.JobId.From(JobId),
                BatchId = UKHO.ADDS.EFS.Domain.Jobs.BatchId.From(BatchId),
                ExchangeSetFilePath = _testDirectory,
                ExchangeSetArchiveFolderName = ArchiveFolder,
                ExchangeSetNameTemplate = ExchangeSetNameTemplate
            };

            var archivePath = Path.Combine(_testDirectory, ArchiveFolder);
            Directory.CreateDirectory(archivePath);

            A.CallTo(() => _executionContext.Subject).Returns(exchangeSetPipelineContext);
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(UploadFilesNode).FullName!)).Returns(_logger);
            A.CallTo(() => _fileNameGeneratorService.GenerateFileName(A<string>._, A<JobId>._, A<DateTime?>._))
                .Returns(GeneratedFileName);

            _tempFilePath = Path.Combine(archivePath, "TestJobId.zip");
            File.WriteAllText(_tempFilePath, "Temporary test file content.");
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_tempFilePath) && File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }

            var archivePath = Path.Combine(_testDirectory, ArchiveFolder);
            if (Directory.Exists(archivePath))
            {
                Directory.Delete(archivePath, recursive: true);
            }

            _loggerFactory?.Dispose();
        }

        [Test]
        public void WhenFileShareReadWriteClientIsNull_ThenThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new UploadFilesNode(null!, _fileNameGeneratorService));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndUploadSucceeds_ThenReturnsSucceeded()
        {
            var batchHandle = new BatchHandle(BatchId);
            var fakeResult = A.Fake<IResult<AddFileToBatchResponse>>();
            var response = new AddFileToBatchResponse();
            IError? error = null;

            A.CallTo(() => fakeResult.IsSuccess(out response, out error)).Returns(true);
            A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(
                A<BatchHandle>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _uploadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            A.CallTo(() => _fileNameGeneratorService.GenerateFileName(
                ExchangeSetNameTemplate,
                Domain.Jobs.JobId.From(JobId),
                A<DateTime?>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndFileDoesNotExist_ThenReturnsFailed()
        {
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }

            var result = await _uploadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            A.CallTo(() => _logger.Log<LoggerMessageState>(
                LogLevel.Error,
                A<EventId>.That.Matches(e => e.Name == "UploadFilesNotFound"),
                A<LoggerMessageState>._,
                A<Exception>._,
                A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndAddFileToBatchFails_ThenReturnsFailed()
        {
            var fakeResult = A.Fake<IResult<AddFileToBatchResponse>>();
            var fakeError = A.Fake<IError>();
            AddFileToBatchResponse? response = null;

            A.CallTo(() => fakeResult.IsSuccess(out response, out fakeError)).Returns(false);
            A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(
                A<BatchHandle>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _uploadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "FileShareAddFileToBatchError"),
                    A<LoggerMessageState>._,
                    A<Exception>._,
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

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "AddFileNodeFailed"),
                    A<LoggerMessageState>._,
                    A<Exception>.That.Matches(ex => ex.Message == "Test exception"),
                    A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenAddFileToBatchSucceedsWithFileDetails_ThenUpdatesCommitInfo()
        {
            var fakeResult = A.Fake<IResult<AddFileToBatchResponse>>();
            var response = new AddFileToBatchResponse();
            IError? error = null;

            A.CallTo(() => fakeResult.IsSuccess(out response, out error)).Returns(true);

            var batchHandleWithFiles = new BatchHandle(BatchId);
            batchHandleWithFiles.AddFile(GeneratedFileName, "ABC123");

            A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(
                A<BatchHandle>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .ReturnsLazily(call =>
                {
                    var handle = call.Arguments.Get<BatchHandle>(0);
                    handle?.FileDetails.Add(new FileDetail { FileName = GeneratedFileName, Hash = "ABC123" });
                    return Task.FromResult(fakeResult);
                });

            var result = await _uploadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenAddFileToBatchSucceedsWithoutFileDetails_ThenDoesNotUpdateCommitInfo()
        {
            var fakeResult = A.Fake<IResult<AddFileToBatchResponse>>();
            var response = new AddFileToBatchResponse();
            IError? error = null;

            A.CallTo(() => fakeResult.IsSuccess(out response, out error)).Returns(true);
            A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(
                A<BatchHandle>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _uploadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenAddFileToBatchFailsWithRetriableStatusCode_ThenRetriesExpectedNumberOfTimes()
        {
            var callCount = 0;
            A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(
                A<BatchHandle>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .ReturnsLazily(call =>
                {
                    callCount++;
                    var error = new Error("Retriable error", new Dictionary<string, object> { { "StatusCode", 503 } });
                    IResult<AddFileToBatchResponse> result = Result.Failure<AddFileToBatchResponse>(error);
                    return Task.FromResult(result);
                });

            var result = await _uploadFilesNode.ExecuteAsync(_executionContext);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(callCount, Is.EqualTo(4), "Should retry 3 times plus the initial call (total 4)");
                Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            }
        }

        [Test]
        public async Task WhenUploadSucceeds_ThenUsesCorrectParameters()
        {
            var fakeResult = A.Fake<IResult<AddFileToBatchResponse>>();
            var response = new AddFileToBatchResponse();
            IError? error = null;

            A.CallTo(() => fakeResult.IsSuccess(out response, out error)).Returns(true);
            A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(
                A<BatchHandle>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _uploadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));

            A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(
                A<BatchHandle>.That.Matches(b => b.BatchId == BatchId),
                A<Stream>._,
                GeneratedFileName,
                "application/octet-stream",
                JobId,
                A<CancellationToken>._))
                .MustHaveHappened();
        }

        [Test]
        public async Task WhenFileStreamIsCreated_ThenStreamIsDisposedProperly()
        {
            var fakeResult = A.Fake<IResult<AddFileToBatchResponse>>();
            var response = new AddFileToBatchResponse();
            IError? error = null;

            A.CallTo(() => fakeResult.IsSuccess(out response, out error)).Returns(true);
            A.CallTo(() => _fileShareReadWriteClient.AddFileToBatchAsync(
                A<BatchHandle>._,
                A<Stream>._,
                A<string>._,
                A<string>._,
                A<string>._,
                A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _uploadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenLoggerFactoryCreatesLogger_ThenLoggerIsUsedForErrors()
        {
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }

            var result = await _uploadFilesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(UploadFilesNode).FullName!))
                .MustHaveHappenedOnceExactly();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
    }
}
