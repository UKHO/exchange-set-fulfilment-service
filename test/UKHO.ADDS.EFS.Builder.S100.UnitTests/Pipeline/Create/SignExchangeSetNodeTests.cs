using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create;
using UKHO.ADDS.EFS.Builder.S100.Services;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Create
{
    [TestFixture]
    public class SignExchangeSetNodeTests
    {
        private IToolClient _toolClient;
        private TestableSignExchangeSetNode _testableSignExchangeSetNode;
        private IExecutionContext<ExchangeSetPipelineContext> _executionContext;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _toolClient = A.Fake<IToolClient>();
            _testableSignExchangeSetNode = new TestableSignExchangeSetNode(_toolClient);
            _executionContext = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
        }

        [SetUp]
        public void SetUp()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var exchangeSetPipelineContext = new ExchangeSetPipelineContext(null, null, null, loggerFactory)
            {
                Job = new ExchangeSetJob { CorrelationId = "TestCorrelationId" },
                JobId = "TestJobId",
                WorkspaceAuthenticationKey = "Test123"
            };
            A.CallTo(() => _executionContext.Subject).Returns(exchangeSetPipelineContext);
        }

        [Test]
        public async Task WhenSignExchangeSetAsyncReturnsSuccess_ThenReturnsSucceeded()
        {
            var signingResponse = new SigningResponse { Certificate = "cert", SigningKey = "key", Status = "ok" };
            var result = Result.Success(signingResponse);
            A.CallTo(() => _toolClient.SignExchangeSetAsync("TestJobId", "Test123", "TestCorrelationId")).Returns(Task.FromResult<IResult<SigningResponse>>(result));

            var status = await _testableSignExchangeSetNode.PerformExecuteAsync(_executionContext);

            Assert.That(status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenSignExchangeSetAsyncReturnsFailure_ThenReturnsFailedAndLogs()
        {
            var error = ErrorFactory.CreateError(HttpStatusCode.BadRequest);
            var result = Result.Failure<SigningResponse>(error);
            A.CallTo(() => _toolClient.SignExchangeSetAsync("TestJobId", "Test123", "TestCorrelationId")).Returns(Task.FromResult<IResult<SigningResponse>>(result));

            var status = await _testableSignExchangeSetNode.PerformExecuteAsync(_executionContext);

            Assert.That(status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public void WhenToolClientIsNull_ThenThrowsArgumentException()
        {
            Assert.That(() => new SignExchangeSetNode(null), Throws.ArgumentException);
        }

        [Test]
        public Task WhenContextSubjectLoggerFactoryIsNull_ThenThrowsNullReferenceException()
        {
            var pipelineContext = new ExchangeSetPipelineContext(A.Fake<Microsoft.Extensions.Configuration.IConfiguration>(), A.Fake<INodeStatusWriter>(), _toolClient, null)
            {
                JobId = "TestJobId",
                WorkspaceAuthenticationKey = "Test123"
            };

            var context = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
            A.CallTo(() => context.Subject).Returns(pipelineContext);

            Assert.That(async () => await _testableSignExchangeSetNode.PerformExecuteAsync(context), Throws.Exception);
            return Task.CompletedTask;
        }

        private class TestableSignExchangeSetNode : SignExchangeSetNode
        {
            public TestableSignExchangeSetNode(IToolClient iToolClient)
                : base(iToolClient)
            {
            }

            public new async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
            {
                return await base.PerformExecuteAsync(context);
            }
        }
    }
}
