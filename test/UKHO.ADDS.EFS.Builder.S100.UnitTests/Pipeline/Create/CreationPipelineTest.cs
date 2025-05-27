using FakeItEasy;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Create
{
    [TestFixture]
    internal class CreationPipelineTest
    {
        private IToolClient _toolClient;
        private CreationPipeline _creationPipeline;
        private ExchangeSetPipelineContext _context;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _toolClient = A.Fake<IToolClient>();
            _creationPipeline = new CreationPipeline(_toolClient);

            _context = A.Fake<ExchangeSetPipelineContext>();
            _context.JobId = "TestJobId";
            _context.WorkspaceAuthenticationKey = "TestAuthKey";
            _context.Job = new ExchangeSetJob { CorrelationId = "TestCorrelationId" };
        }

        [Test]
        public async Task WhenExecutePipelineIsCalled_ThenAllNodesAreExecutedAndReturnsNodeResult()
        {
            var result = await _creationPipeline.ExecutePipeline(_context);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void WhenToolClientIsNull_ThenThrowsArgumentNullException()
        {
            Assert.That(() => new CreationPipeline(null), Throws.ArgumentNullException);
        }

        [Test]
        public void WhenContextIsNull_ThenThrowsArgumentNullException()
        {
            Assert.That(async () => await _creationPipeline.ExecutePipeline(null), Throws.ArgumentException);
        }

        [Test]
        public async Task WhenJobIdIsNull_ThenReturnsFailedNodeResult()
        {
            _context.JobId = null;
            var result = await _creationPipeline.ExecutePipeline(_context);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenWorkspaceAuthenticationKeyIsNull_ThenReturnsFailedNodeResult()
        {
            _context.WorkspaceAuthenticationKey = null;
            var result = await _creationPipeline.ExecutePipeline(_context);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenJobIsNull_ThenReturnsFailedNodeResult()
        {
            _context.Job = null;
            var result = await _creationPipeline.ExecutePipeline(_context);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenCorrelationIdIsNull_ThenReturnsFailedNodeResult()
        {
            _context.Job = new ExchangeSetJob { CorrelationId = null };
            var result = await _creationPipeline.ExecutePipeline(_context);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenCreateExchangeSetNodeFails_ThenReturnsFailedNodeResult()
        {
            A.CallTo(() => _toolClient.AddExchangeSetAsync("TestJobId", "TestAuthKey", "TestCorrelationId"))
                .Throws<Exception>();
            var result = await _creationPipeline.ExecutePipeline(_context);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenAddContentExchangeSetNodeFails_ThenReturnsFailedNodeResult()
        {
            A.CallTo(() => _toolClient.AddContentAsync(A<string>._, A<string>._, A<string>._, A<string>._))
                .Throws<Exception>();
            var result = await _creationPipeline.ExecutePipeline(_context);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenSignExchangeSetNodeFails_ThenReturnsFailedNodeResult()
        {
            A.CallTo(() => _toolClient.SignExchangeSetAsync("TestJobId", "TestAuthKey", "TestCorrelationId"))
                .Throws<Exception>();
            var result = await _creationPipeline.ExecutePipeline(_context);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }
    }
}
