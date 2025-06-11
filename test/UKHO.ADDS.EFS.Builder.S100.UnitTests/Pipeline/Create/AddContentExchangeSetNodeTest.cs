using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
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
        private AddContentExchangeSetNode _addContentExchangeSetNode;
        private IExecutionContext<ExchangeSetPipelineContext> _executionContext;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _toolClient = A.Fake<IToolClient>();
            _addContentExchangeSetNode = new AddContentExchangeSetNode();
            _executionContext = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            _logger = A.Fake<ILogger<AddContentExchangeSetNode>>();
        }

        [SetUp]
        public void Setup()
        {
            var exchangeSetPipelineContext = new ExchangeSetPipelineContext(null, null, _toolClient, _loggerFactory)
            {
                Job = new ExchangeSetJob { CorrelationId = "TestCorrelationId" },
                JobId = "TestJobId",
                WorkspaceAuthenticationKey = "Test123",
                WorkSpaceRootPath = "rootPath"
            };
            A.CallTo(() => _executionContext.Subject).Returns(exchangeSetPipelineContext);
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(AddContentExchangeSetNode).FullName!)).Returns(_logger);
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
        }
    }
}
