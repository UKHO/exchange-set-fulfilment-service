using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Create
{
    [TestFixture]
    internal class AddContentExchangeSetNodeTest
    {
        private IToolClient _toolClient;
        private AddContentExchangeSetNode _addContentExchangeSetNode;
        private IExecutionContext<S100ExchangeSetPipelineContext> _executionContext;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;
        private string _testDirectory;

        private const string JobId = "TestJobId";
        private const string BatchId = "a-valid-batch-id";
        private const string WorkspaceAuthKey = "Test123";
        private const string SpoolFolder = "spool";
        private const string DataSetFilesFolder = "dataSet_files";
        private const string SupportFilesFolder = "support_files";
        private static readonly List<string> _defaultBatchFileNameDetails = ["101GBTest1_1_0", "102GBTest2_1_0"];

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        [SetUp]
        public void Setup()
        {
            _toolClient = A.Fake<IToolClient>();
            _addContentExchangeSetNode = new AddContentExchangeSetNode();
            _executionContext = A.Fake<IExecutionContext<S100ExchangeSetPipelineContext>>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            _logger = A.Fake<ILogger<AddContentExchangeSetNode>>();

            var exchangeSetPipelineContext = new S100ExchangeSetPipelineContext(null!, _toolClient, null!, null!, _loggerFactory)
            {
                Build = new S100Build
                {
                    JobId = Domain.Jobs.JobId.From(JobId),
                    BatchId = Domain.Jobs.BatchId.From(BatchId),
                    DataStandard = DataStandard.S100,
                },
                JobId = Domain.Jobs.JobId.From(JobId),
                WorkspaceAuthenticationKey = WorkspaceAuthKey,
                WorkSpaceRootPath = _testDirectory,
                BatchFileNameDetails = _defaultBatchFileNameDetails
            };

            var spoolPath = Path.Combine(_testDirectory, SpoolFolder);
            var datasetFilesPath = Path.Combine(spoolPath, DataSetFilesFolder);
            var supportFilesPath = Path.Combine(spoolPath, SupportFilesFolder);

            Directory.CreateDirectory(datasetFilesPath);
            Directory.CreateDirectory(supportFilesPath);

            A.CallTo(() => _executionContext.Subject).Returns(exchangeSetPipelineContext);
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(AddContentExchangeSetNode).FullName!)).Returns(_logger);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            _loggerFactory?.Dispose();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndAddContentSucceeds_ThenReturnSucceeded()
        {
            CreateRequiredDirectoryStructure();
            var fakeResult = A.Fake<IResult<OperationResponse>>();
            var opResponse = new OperationResponse { Code = 0, Type = "Success", Message = "OK" };
            IError? error = null;
            A.CallTo(() => fakeResult.IsSuccess(out opResponse, out error)).Returns(true);
            A.CallTo(() => _toolClient.AddContentAsync(A<string>._, A<JobId>._, A<string>._))
                .Returns(Task.FromResult(fakeResult));
            var result = await _addContentExchangeSetNode.ExecuteAsync(_executionContext);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            A.CallTo(() => _toolClient.AddContentAsync($"{_defaultBatchFileNameDetails[0]}/S100_ROOT/CATALOG.XML", Domain.Jobs.JobId.From(JobId), WorkspaceAuthKey))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _toolClient.AddContentAsync($"{_defaultBatchFileNameDetails[1]}/S100_ROOT/CATALOG.XML", Domain.Jobs.JobId.From(JobId), WorkspaceAuthKey))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndAddContentFails_ThenReturnFailed()
        {
            CreateRequiredDirectoryStructure();
            var fakeResult = A.Fake<IResult<OperationResponse>>();
            var fakeError = A.Fake<IError>();
            OperationResponse? opResponse = null;
            A.CallTo(() => fakeResult.IsSuccess(out opResponse, out fakeError)).Returns(false);
            A.CallTo(() => _toolClient.AddContentAsync(A<string>._, A<JobId>._, A<string>._))
                .Returns(Task.FromResult(fakeResult));
            var result = await _addContentExchangeSetNode.ExecuteAsync(_executionContext);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "AddContentExchangeSetNodeFailed"),
                    A<LoggerMessageState>._,
                    A<Exception>._,
                    A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndFirstAddContentSucceedsButSecondFails_ThenReturnFailed()
        {
            CreateRequiredDirectoryStructure();
            var successResult = A.Fake<IResult<OperationResponse>>();
            var opResponse = new OperationResponse { Code = 0, Type = "Success", Message = "OK" };
            IError? successError = null;
            A.CallTo(() => successResult.IsSuccess(out opResponse, out successError)).Returns(true);
            var failureResult = A.Fake<IResult<OperationResponse>>();
            var fakeError = A.Fake<IError>();
            OperationResponse? failureOpResponse = null;
            A.CallTo(() => failureResult.IsSuccess(out failureOpResponse, out fakeError)).Returns(false);
            A.CallTo(() => _toolClient.AddContentAsync($"{_defaultBatchFileNameDetails[0]}/S100_ROOT/CATALOG.XML", A<JobId>._, A<string>._))
                .Returns(Task.FromResult(successResult));
            A.CallTo(() => _toolClient.AddContentAsync($"{_defaultBatchFileNameDetails[1]}/S100_ROOT/CATALOG.XML", A<JobId>._, A<string>._))
                .Returns(Task.FromResult(failureResult));
            var result = await _addContentExchangeSetNode.ExecuteAsync(_executionContext);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            A.CallTo(() => _toolClient.AddContentAsync($"{_defaultBatchFileNameDetails[0]}/S100_ROOT/CATALOG.XML", Domain.Jobs.JobId.From(JobId), WorkspaceAuthKey))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _toolClient.AddContentAsync($"{_defaultBatchFileNameDetails[1]}/S100_ROOT/CATALOG.XML", Domain.Jobs.JobId.From(JobId), WorkspaceAuthKey))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "AddContentExchangeSetNodeFailed"),
                    A<LoggerMessageState>._,
                    A<Exception>._,
                    A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndNoBatchDirectoriesExist_ThenReturnSucceeded()
        {
            var result = await _addContentExchangeSetNode.ExecuteAsync(_executionContext);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            A.CallTo(() => _toolClient.AddContentAsync(A<string>._, A<JobId>._, A<string>._))
                .MustNotHaveHappened();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndOnlyFirstBatchDirectoryExists_ThenReturnSucceededButNoCalls()
        {
            var spoolPath = Path.Combine(_testDirectory, SpoolFolder);
            var firstBatchPath = Path.Combine(spoolPath, _defaultBatchFileNameDetails[0]);
            Directory.CreateDirectory(firstBatchPath);
            var result = await _addContentExchangeSetNode.ExecuteAsync(_executionContext);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            A.CallTo(() => _toolClient.AddContentAsync(A<string>._, A<JobId>._, A<string>._))
                .MustNotHaveHappened();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledWithEmptyBatchFileNameDetails_ThenReturnSucceeded()
        {
            _executionContext.Subject.BatchFileNameDetails = [];
            var result = await _addContentExchangeSetNode.ExecuteAsync(_executionContext);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledWithSingleBatchFile_ThenReturnSucceeded()
        {
            _executionContext.Subject.BatchFileNameDetails = ["SingleBatch_1_0"];
            var spoolPath = Path.Combine(_testDirectory, SpoolFolder);
            var batchPath = Path.Combine(spoolPath, "SingleBatch_1_0");
            Directory.CreateDirectory(batchPath);
            var fakeResult = A.Fake<IResult<OperationResponse>>();
            var opResponse = new OperationResponse { Code = 0, Type = "Success", Message = "OK" };
            IError? error = null;
            A.CallTo(() => fakeResult.IsSuccess(out opResponse, out error)).Returns(true);
            A.CallTo(() => _toolClient.AddContentAsync(A<string>._, A<JobId>._, A<string>._))
                .Returns(Task.FromResult(fakeResult));
            var result = await _addContentExchangeSetNode.ExecuteAsync(_executionContext);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            A.CallTo(() => _toolClient.AddContentAsync("SingleBatch_1_0/S100_ROOT/CATALOG.XML", Domain.Jobs.JobId.From(JobId), WorkspaceAuthKey))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndLoggerFactoryCreatesLogger_ThenLoggerIsUsedForErrors()
        {
            CreateRequiredDirectoryStructure();
            var fakeResult = A.Fake<IResult<OperationResponse>>();
            var fakeError = A.Fake<IError>();
            OperationResponse? opResponse = null;
            A.CallTo(() => fakeResult.IsSuccess(out opResponse, out fakeError)).Returns(false);
            A.CallTo(() => _toolClient.AddContentAsync(A<string>._, A<JobId>._, A<string>._))
                .Returns(Task.FromResult(fakeResult));
            var result = await _addContentExchangeSetNode.ExecuteAsync(_executionContext);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(AddContentExchangeSetNode).FullName!))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "AddContentExchangeSetNodeFailed"),
                    A<LoggerMessageState>._,
                    A<Exception>._,
                    A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappenedOnceExactly();
        }

        private void CreateRequiredDirectoryStructure()
        {
            var spoolPath = Path.Combine(_testDirectory, SpoolFolder);
            foreach (var batchFileName in _executionContext.Subject.BatchFileNameDetails)
            {
                var batchPath = Path.Combine(spoolPath, batchFileName);
                Directory.CreateDirectory(batchPath);
            }
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
