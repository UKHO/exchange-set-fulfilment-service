using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline
{
    [TestFixture]
    internal class CreationPipelineTest
    {
        private IToolClient _toolClient;
        private CreationPipeline _creationPipeline;
        private IExecutionContext<S100ExchangeSetPipelineContext> _context;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _toolClient = A.Fake<IToolClient>();
            _context = A.Fake<IExecutionContext<S100ExchangeSetPipelineContext>>();
            _creationPipeline = new CreationPipeline();
        }

        [SetUp]
        public void Setup()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var exchangeSetPipelineContext = new S100ExchangeSetPipelineContext(null!, _toolClient, null!, null!, loggerFactory)
            {
                Build = new S100Build
                {
                    JobId = JobId.From("TestCorrelationId"),
                    BatchId = BatchId.From("a-valid-batch-id"),
                    DataStandard = DataStandard.S100
                },
                JobId = JobId.From("TestJobId"),
                WorkspaceAuthenticationKey = "Test123",
                BatchFileNameDetails = ["101GBTest1_1_0", "102GBTest2_1_0"]
            };

            A.CallTo(() => _context.Subject).Returns(exchangeSetPipelineContext);
        }

        [Test]
        public async Task WhenAllPipelineNodesSucceed_ThenReturnsSuccessNodeResult()
        {
            var fakeAddExchangeSetResult = A.Fake<IResult<OperationResponse>>();
            var opResponse = new OperationResponse { Code = 0, Type = "Success", Message = "OK" };
            IError? error = null;
            A.CallTo(() => fakeAddExchangeSetResult.IsSuccess(out opResponse, out error)).Returns(true);
            A.CallTo(() => _toolClient.AddExchangeSetAsync(_context.Subject.JobId, _context.Subject.WorkspaceAuthenticationKey)).Returns(Task.FromResult(fakeAddExchangeSetResult));

            var fakeAddContentResult = A.Fake<IResult<OperationResponse>>();
            A.CallTo(() => fakeAddContentResult.IsSuccess(out opResponse, out error)).Returns(true);
            A.CallTo(() => _toolClient.AddContentAsync(A<string>._, A<JobId>._, A<string>._))
                .Returns(Task.FromResult(fakeAddContentResult));

            var fakeSignResult = A.Fake<IResult<SigningResponse>>();
            var signingResponse = new SigningResponse { Certificate = "cert", SigningKey = "key", Status = "Success" };
            IError? signError = null;
            A.CallTo(() => fakeSignResult.IsSuccess(out signingResponse, out signError)).Returns(true);
            A.CallTo(() => _toolClient.SignExchangeSetAsync(A<JobId>._, A<string>._)).Returns(Task.FromResult(fakeSignResult));

            var result = await _creationPipeline.ExecutePipeline(_context.Subject);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));

            A.CallTo(() => _toolClient.AddExchangeSetAsync(A<JobId>._, A<string>._))
                .MustHaveHappened();

            A.CallTo(() => _toolClient.SignExchangeSetAsync(A<JobId>._, A<string>._))
                .MustHaveHappened();
        }

        [Test]
        public void WhenContextIsNull_ThenThrowsArgumentException()
        {
            Assert.That(async () => await _creationPipeline.ExecutePipeline(null!), Throws.ArgumentException);
        }

        [Test]
        public async Task WhenRequiredContextPropertiesAreNull_ToolClient_ThenReturnsFailedNodeResult()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var exchangeSetPipelineContext = new S100ExchangeSetPipelineContext(null!, null!, null!, null!, loggerFactory);
            A.CallTo(() => _context.Subject).Returns(exchangeSetPipelineContext);

            var result = await _creationPipeline.ExecutePipeline(_context.Subject);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenJobIsNull_ThenReturnsFailedNodeResult()
        {
            _context.Subject.Build = null!;
            var result = await _creationPipeline.ExecutePipeline(_context.Subject);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenCreateExchangeSetNodeFails_ThenReturnsFailedNodeResult()
        {
            A.CallTo(() => _toolClient.AddExchangeSetAsync(JobId.From("TestJobId"), "Test123"))
                .Throws<Exception>();
            var result = await _creationPipeline.ExecutePipeline(_context.Subject);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenAddContentExchangeSetNodeFails_ThenReturnsFailedNodeResult()
        {
            A.CallTo(() => _toolClient.AddContentAsync(A<string>._, A<JobId>._, A<string>._))
                .Throws<Exception>();

            var result = await _creationPipeline.ExecutePipeline(_context.Subject);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenSignExchangeSetNodeFails_ThenReturnsFailedNodeResult()
        {
            A.CallTo(() => _toolClient.SignExchangeSetAsync(JobId.From("TestJobId"), "Test123"))
                .Throws<Exception>();
            var result = await _creationPipeline.ExecutePipeline(_context.Subject);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }
    }
}
