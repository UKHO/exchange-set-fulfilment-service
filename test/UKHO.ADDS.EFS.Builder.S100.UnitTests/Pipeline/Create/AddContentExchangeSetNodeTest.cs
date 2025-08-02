using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Jobs;
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

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _toolClient = A.Fake<IToolClient>();
            _addContentExchangeSetNode = new AddContentExchangeSetNode();
            _executionContext = A.Fake<IExecutionContext<S100ExchangeSetPipelineContext>>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            _logger = A.Fake<ILogger<AddContentExchangeSetNode>>();
            // Create a temporary directory for tests
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        [SetUp]
        public void Setup()
        {
            var exchangeSetPipelineContext = new S100ExchangeSetPipelineContext(null, _toolClient, null, null, _loggerFactory)
            {
                Build = new S100Build
                {
                    JobId = "TestCorrelationId",
                    BatchId = "a-valid-batch-id",
                    DataStandard = DataStandard.S100
                },
                JobId = "TestJobId",
                WorkspaceAuthenticationKey = "Test123",
                WorkSpaceRootPath = _testDirectory // Use temp directory instead of hardcoded path
            };

            // Create the required directory structure
            var spoolPath = Path.Combine(_testDirectory, "spool");
            var datasetFilesPath = Path.Combine(spoolPath, "dataSet_files");
            var supportFilesPath = Path.Combine(spoolPath, "support_files");

            Directory.CreateDirectory(datasetFilesPath);
            Directory.CreateDirectory(supportFilesPath);

            A.CallTo(() => _executionContext.Subject).Returns(exchangeSetPipelineContext);
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(AddContentExchangeSetNode).FullName!)).Returns(_logger);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test directories after each test
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndAddContentSucceeds_ThenReturnSucceeded()
        {
            var fakeResult = A.Fake<IResult<OperationResponse>>();
            var opResponse = new OperationResponse { Code = 0, Type = "Success", Message = "OK" };
            IError? error = null;

            A.CallTo(() => fakeResult.IsSuccess(out opResponse, out error)).Returns(true);

            A.CallTo(() => _toolClient.AddContentAsync(A<string>._, A<string>._, A<string>._, A<string>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _addContentExchangeSetNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndAddContentFails_ThenReturnsFailed()
        {
            A.CallTo(() => _toolClient.AddContentAsync(A<string>._, A<string>._, A<string>._, A<string>._))
                .Returns(Result.Failure<OperationResponse>("error"));

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

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _loggerFactory?.Dispose();

            // Final cleanup of temp directory
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
    }
}
