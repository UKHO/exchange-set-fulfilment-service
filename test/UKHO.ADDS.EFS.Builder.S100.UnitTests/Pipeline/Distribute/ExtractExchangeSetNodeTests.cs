using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute;
using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Distribute
{
    [TestFixture]
    internal class ExtractExchangeSetNodeTests
    {
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;
        private IExecutionContext<S100ExchangeSetPipelineContext> _executionContext;
        private S100ExchangeSetPipelineContext _pipelineContext;
        private ExtractExchangeSetNode _extractExchangeSetNode;
        private IToolClient _toolClient;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _toolClient = A.Fake<IToolClient>();
            _extractExchangeSetNode = new ExtractExchangeSetNode();
            _executionContext = A.Fake<IExecutionContext<S100ExchangeSetPipelineContext>>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            _logger = A.Fake<ILogger<ExtractExchangeSetNode>>();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _loggerFactory?.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            _pipelineContext = new S100ExchangeSetPipelineContext(null, _toolClient, null, null, _loggerFactory)
            {
                Job = new S100ExchangeSetJob { Id = "testId" },
                WorkspaceAuthenticationKey = "authKey"
            };

            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(ExtractExchangeSetNode).FullName!)).Returns(_logger);
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIICResultIsSuccess_ThenReturnSucceeded()
        {
            var fakeStream = new MemoryStream();
            var fakeResult = A.Fake<IResult<Stream>>();
            Stream outStream = fakeStream;
            IError outError = null;

            A.CallTo(() => fakeResult.IsFailure(out outError!, out outStream!)).Returns(false);
            A.CallTo(() => _executionContext.Subject.ToolClient.ExtractExchangeSetAsync(A<string>._, A<string>._, A<string>._, A<string>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _extractExchangeSetNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            //TODO: check for failing log message later
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIICResultIsFailed_ThenReturnFailed()
        {
            var fakeError = A.Fake<IError>();
            var fakeResult = A.Fake<IResult<Stream>>();
            Stream outStream = null;

            A.CallTo(() => fakeResult.IsFailure(out fakeError, out outStream!)).Returns(true);
            A.CallTo(() => _executionContext.Subject.ToolClient.ExtractExchangeSetAsync(A<string>._, A<string>._, A<string>._, A<string>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _extractExchangeSetNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "IICExtractExchangeSetError"),
                    A<LoggerMessageState>._,
                    A<Exception>._,
                    A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncThrowsException_ThenReturnFailed()
        {
            var exceptionMessage = "Extract exchange set failed";
            string loggedMessage = null;

            A.CallTo(() => _executionContext.Subject.ToolClient.ExtractExchangeSetAsync(A<string>._, A<string>._, A<string>._, A<string>._))
                .Throws(new Exception(exceptionMessage));

            var result = await _extractExchangeSetNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "ExtractExchangeSetNodeFailed"),
                    A<LoggerMessageState>._,
                    A<Exception>._,
                    A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappenedOnceExactly();
        }
    }
}

