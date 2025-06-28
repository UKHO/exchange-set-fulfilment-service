using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create;
using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Create
{
    [TestFixture]
    public class SignExchangeSetNodeTests
    {
        private IToolClient _toolClient;
        private SignExchangeSetNode _signExchangeSetNode;
        private IExecutionContext<ExchangeSetPipelineContext> _executionContext;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _toolClient = A.Fake<IToolClient>();
            _signExchangeSetNode = new SignExchangeSetNode();
            _executionContext = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            _logger = A.Fake<ILogger<SignExchangeSetNode>>();
        }

        [SetUp]
        public void SetUp()
        {
            var exchangeSetPipelineContext = new ExchangeSetPipelineContext(null, null, _toolClient, _loggerFactory)
            {
                Job = new S100ExchangeSetJob { CorrelationId = "TestCorrelationId" },
                JobId = "TestJobId",
                WorkspaceAuthenticationKey = "Test123"
            };
            A.CallTo(() => _executionContext.Subject).Returns(exchangeSetPipelineContext);
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(SignExchangeSetNode).FullName!)).Returns(_logger);
        }

        [Test]
        public async Task WhenSignExchangeSetAsyncReturnsSuccess_ThenReturnsSucceeded()
        {
            var signingResponse = new SigningResponse { Certificate = "cert", SigningKey = "key", Status = "ok" };
            var result = Result.Success(signingResponse);
            A.CallTo(() => _toolClient.SignExchangeSetAsync("TestJobId", "Test123", "TestCorrelationId")).Returns(Task.FromResult<IResult<SigningResponse>>(result));
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(SignExchangeSetNode).FullName!)).Returns(_logger);

            var nodeResult = await _signExchangeSetNode.ExecuteAsync(_executionContext);

            Assert.That(nodeResult.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenSignExchangeSetAsyncReturnsFailure_ThenReturnsFailedAndLogs()
        {
            var error = ErrorFactory.CreateError(HttpStatusCode.BadRequest);
            var result = Result.Failure<SigningResponse>(error);
            A.CallTo(() => _toolClient.SignExchangeSetAsync("TestJobId", "Test123", "TestCorrelationId")).Returns(Task.FromResult<IResult<SigningResponse>>(result));
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(SignExchangeSetNode).FullName!)).Returns(_logger);

            var nodeResult = await _signExchangeSetNode.ExecuteAsync(_executionContext);

            Assert.That(nodeResult.Status, Is.EqualTo(NodeResultStatus.Failed));
            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "SignExchangeSetNodeFailed"),
                    A<LoggerMessageState>._,
                    A<Exception>._,
                    A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappenedOnceExactly();
        }

        

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _loggerFactory?.Dispose();
        }
    }
}
