using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Create
{
    [TestFixture]
    internal class CreateExchangeSetNodeTest
    {
        private IToolClient _toolClient;
        private TestableCreateExchangeSetNode _testableCreateExchangeSetNode;
        private IExecutionContext<ExchangeSetPipelineContext> _executionContext;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _toolClient = A.Fake<IToolClient>();
            _testableCreateExchangeSetNode = new TestableCreateExchangeSetNode(_toolClient);
            _executionContext = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
        }

        [SetUp]
        public void Setup()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

            var exchangeSetPipelineContext = new ExchangeSetPipelineContext(null, null, null, loggerFactory)
            {
                Job = new ExchangeSetJob { CorrelationId = "TestCorrelationId" },
                JobId = "TestJobId",
                WorkspaceAuthenticationKey = "TestAuthKey"
            };

            A.CallTo(() => _executionContext.Subject).Returns(exchangeSetPipelineContext);
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndAddExchangeSetSucceeds_ThenReturnsSucceeded()
        {
            var fakeResult = A.Fake<IResult<OperationResponse>>();
            var opResponse = new OperationResponse { Code = 0, Type = "Success", Message = "OK" };
            IError error = null;

            A.CallTo(() => fakeResult.IsSuccess(out opResponse, out error))
                .Returns(true);

            A.CallTo(() => _toolClient.AddExchangeSetAsync(A<string>._, A<string>._, A<string>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _testableCreateExchangeSetNode.PerformExecuteAsync(_executionContext);

            Assert.That(result, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndAddExchangeSetFails_ThenReturnsFailed()
        {
            OperationResponse value = default!;
            var fakeResult = A.Fake<IResult<OperationResponse>>();
            A.CallTo(() => _toolClient.AddExchangeSetAsync(A<string>._, A<string>._, A<string>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _testableCreateExchangeSetNode.PerformExecuteAsync(_executionContext);

            Assert.That(result, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public void WhenToolClientIsNull_ThenThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CreateExchangeSetNode(null));
        }

        private class TestableCreateExchangeSetNode : CreateExchangeSetNode
        {
            public TestableCreateExchangeSetNode(IToolClient toolClient)
                : base(toolClient)
            {
            }

            public new async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
            {
                return await base.PerformExecuteAsync(context);
            }
        }
    }
}
