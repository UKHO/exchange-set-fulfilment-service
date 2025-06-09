using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute;
using UKHO.ADDS.EFS.Entities;
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
        private IExecutionContext<ExchangeSetPipelineContext> _executionContext;
        private ExchangeSetPipelineContext _pipelineContext;
        private ExtractExchangeSetNode _extractExchangeSetNode;
        private IToolClient _toolClient;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _toolClient = A.Fake<IToolClient>();
            _extractExchangeSetNode = new ExtractExchangeSetNode();
            _executionContext = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
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
            _pipelineContext = new ExchangeSetPipelineContext(null, null, _toolClient, _loggerFactory)
            {
                Job = new ExchangeSetJob { Id = "testId", CorrelationId = "corrId" },
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
            //TODO: will check for failing log message later
            //A.CallTo(() => _logger.Log<LoggerMessageState>(
            //        LogLevel.Error,
            //        A<EventId>.That.Matches(e => e.Name == "IICExtractExchangeSetError"),
            //        A<LoggerMessageState>._,
            //        null,
            //        A<Func<LoggerMessageState, Exception?, string>>._))
            //    .MustNotHaveHappened();
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
                    null,
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

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "ExtractExchangeSetNodeFailed"),
                    A<LoggerMessageState>._,
                    null,
                    A<Func<LoggerMessageState, Exception?, string>>._))
                .Invokes((LogLevel level, EventId eventId, LoggerMessageState state, Exception ex, Func<LoggerMessageState, Exception?, string> formatter) =>
                {
                    loggedMessage = formatter(state, ex);
                });

            var result = await _extractExchangeSetNode.ExecuteAsync(_executionContext);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
                Assert.That(loggedMessage, Does.Contain(exceptionMessage));
            });
        }
    }
}

