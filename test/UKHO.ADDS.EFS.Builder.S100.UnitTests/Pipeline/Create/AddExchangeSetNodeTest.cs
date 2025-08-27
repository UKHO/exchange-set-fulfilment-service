using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Implementation;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Create
{
    [TestFixture]
    internal class AddExchangeSetNodeTest
    {
        private IToolClient _toolClient;
        private AddExchangeSetNode _addExchangeSetNode;
        private IExecutionContext<S100ExchangeSetPipelineContext> _executionContext;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _toolClient = A.Fake<IToolClient>();
            _addExchangeSetNode = new AddExchangeSetNode();
            _executionContext = A.Fake<IExecutionContext<S100ExchangeSetPipelineContext>>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            _logger = A.Fake<ILogger<AddContentExchangeSetNode>>();
        }

        [SetUp]
        public void Setup()
        {
            var exchangeSetPipelineContext = new S100ExchangeSetPipelineContext(null, _toolClient, null, null, _loggerFactory)
            {
                Build= new S100Build
                {
                    // TODO JobId == CorrelationId

                    JobId = JobId.From("TestCorrelationId"),
                    BatchId = BatchId.From("a-batch-id"),
                    DataStandard = DataStandard.S100
                },
                JobId = JobId.From("TestJobId"),
                WorkspaceAuthenticationKey = "Test123"
            };

            A.CallTo(() => _executionContext.Subject).Returns(exchangeSetPipelineContext);
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(AddExchangeSetNode).FullName!)).Returns(_logger);
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndAddExchangeSetSucceeds_ThenReturnsSucceeded()
        {
            var fakeResult = A.Fake<IResult<OperationResponse>>();
            var opResponse = new OperationResponse { Code = 0, Type = "Success", Message = "OK" };
            IError error = null;

            A.CallTo(() => fakeResult.IsSuccess(out opResponse, out error))
                .Returns(true);

            A.CallTo(() => _toolClient.AddExchangeSetAsync(A<JobId>._, A<string>._, A<CorrelationId>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _addExchangeSetNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndAddExchangeSetFails_ThenReturnsFailed()
        {
            OperationResponse value = default!;
            var fakeResult = A.Fake<IResult<OperationResponse>>();
            A.CallTo(() => _toolClient.AddExchangeSetAsync(A<JobId>._, A<string>._, A<CorrelationId>._))
                .Returns(Task.FromResult(fakeResult));

            var result = await _addExchangeSetNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "AddExchangeSetNodeFailed"),
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
