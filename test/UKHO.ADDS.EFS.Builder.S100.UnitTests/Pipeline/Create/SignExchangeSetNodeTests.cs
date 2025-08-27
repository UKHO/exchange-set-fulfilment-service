using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.VOS;
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
        private IExecutionContext<S100ExchangeSetPipelineContext> _executionContext;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _toolClient = A.Fake<IToolClient>();
            _signExchangeSetNode = new SignExchangeSetNode();
            _executionContext = A.Fake<IExecutionContext<S100ExchangeSetPipelineContext>>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            _logger = A.Fake<ILogger<SignExchangeSetNode>>();
        }

        [SetUp]
        public void SetUp()
        {
            var exchangeSetPipelineContext = new S100ExchangeSetPipelineContext(null, _toolClient, null, null, _loggerFactory)
            {
                Build = new S100Build()
                {
                    JobId = JobId.From("TestCorrelationId"),
                    BatchId = BatchId.From("a-valid-batch-id"),
                    DataStandard = DataStandard.S100
                },
                JobId = JobId.From("TestJobId"),
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
            A.CallTo(() => _toolClient.SignExchangeSetAsync(JobId.From("TestJobId"), "Test123", CorrelationId.From("TestCorrelationId"))).Returns(Task.FromResult<IResult<SigningResponse>>(result));
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(SignExchangeSetNode).FullName!)).Returns(_logger);

            var nodeResult = await _signExchangeSetNode.ExecuteAsync(_executionContext);

            Assert.That(nodeResult.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenSignExchangeSetAsyncReturnsFailure_ThenReturnsFailedAndLogs()
        {
            var error = ErrorFactory.CreateError(HttpStatusCode.BadRequest);
            var result = Result.Failure<SigningResponse>(error);
            A.CallTo(() => _toolClient.SignExchangeSetAsync(JobId.From("TestJobId"), "Test123", CorrelationId.From("TestCorrelationId"))).Returns(Task.FromResult<IResult<SigningResponse>>(result));
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
