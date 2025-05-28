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
        private IToolClient _toolClient;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;
        private IExecutionContext<ExchangeSetPipelineContext> _executionContext;
        private ExchangeSetPipelineContext _pipelineContext;
        private TestableExtractExchangeSetNode _testableExtractExchangeSetNode;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _toolClient = A.Fake<IToolClient>();
            _testableExtractExchangeSetNode = new TestableExtractExchangeSetNode(_toolClient);
            _executionContext = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            _logger = A.Fake<ILogger>();
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

            A.CallTo(() => fakeResult.IsSuccess(out outStream!, out outError!)).Returns(true);
            A.CallTo(() => _toolClient.ExtractExchangeSetAsync(A<string>._, A<string>._, A<string>._)).Returns(Task.FromResult(fakeResult));

            var status = await _testableExtractExchangeSetNode.PerformExecuteAsync(_executionContext);

            Assert.Multiple(() =>
            {
                Assert.That(_executionContext.Subject.ExchangeSetStream, Is.Not.Null);
                Assert.That(status, Is.EqualTo(NodeResultStatus.Succeeded));
            });
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIICResultIsFailed_ThenReturnFailed()
        {
            var fakeError = A.Fake<IError>();
            var fakeResult = A.Fake<IResult<Stream>>();
            Stream outStream = null;

            A.CallTo(() => fakeResult.IsSuccess(out outStream!, out fakeError)).Returns(false);
            A.CallTo(() => _toolClient.ExtractExchangeSetAsync(A<string>._, A<string>._, A<string>._)).Returns(Task.FromResult(fakeResult));

            var status = await _testableExtractExchangeSetNode.PerformExecuteAsync(_executionContext);

            Assert.Multiple(() =>
            {
                Assert.That(_executionContext.Subject.ExchangeSetStream, Is.Null);
                Assert.That(status, Is.EqualTo(NodeResultStatus.Failed));
            });
        }

        [Test]
        public async Task WhenPerformExecuteAsyncThrowsException_ThenReturnFailed()
        {
            A.CallTo(() => _toolClient.ExtractExchangeSetAsync(A<string>._, A<string>._, A<string>._)).Throws(new Exception("fail"));

            var status = await _testableExtractExchangeSetNode.PerformExecuteAsync(_executionContext);

            Assert.That(status, Is.EqualTo(NodeResultStatus.Failed));
        }

        private class TestableExtractExchangeSetNode : ExtractExchangeSetNode
        {
            public TestableExtractExchangeSetNode(IToolClient toolClient) : base(toolClient) { }

            public new async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
            {
                return await base.PerformExecuteAsync(context);
            }
        }
    }

    
}

