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
    internal class AddContentExchangeSetNodeTest
    {
        private IToolClient _toolClient;
        private TestableAddContentExchangeSetNode _testableNode;
        private IExecutionContext<ExchangeSetPipelineContext> _executionContext;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _toolClient = A.Fake<IToolClient>();
            _testableNode = new TestableAddContentExchangeSetNode();
            _executionContext = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
        }

        [SetUp]
        public void Setup()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var exchangeSetPipelineContext = new ExchangeSetPipelineContext(null, null, _toolClient, loggerFactory)
            {
                Job = new ExchangeSetJob { CorrelationId = "TestCorrelationId" },
                JobId = "TestJobId",
                WorkspaceAuthenticationKey = "Test123",
                WorkSpaceRootPath = "rootPath"
            };
            A.CallTo(() => _executionContext.Subject).Returns(exchangeSetPipelineContext);
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

            var result = await _testableNode.PerformExecuteAsync(_executionContext);

            Assert.That(result, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndAddContentFails_ThenReturnsFailed()
        {
            A.CallTo(() => _toolClient.AddContentAsync(A<string>._, A<string>._, A<string>._, A<string>._))
                .Returns(Result.Failure<OperationResponse>("error"));

            var result = await _testableNode.PerformExecuteAsync(_executionContext);

            Assert.That(result, Is.EqualTo(NodeResultStatus.Failed));
        }

        private class TestableAddContentExchangeSetNode : AddContentExchangeSetNode
        {
            public new async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
            {
                return await base.PerformExecuteAsync(context);
            }
        }
    }
}
