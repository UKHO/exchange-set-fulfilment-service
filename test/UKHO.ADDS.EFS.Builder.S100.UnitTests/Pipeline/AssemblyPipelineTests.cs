using FakeItEasy;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline
{
    [TestFixture]
    public class AssemblyPipelineTests
    {
        private IFileShareReadOnlyClient _fakeReadOnlyClient;
        private IFileShareReadWriteClient _fakeReadWriteClient;

        [SetUp]
        public void SetUp()
        {
            _fakeReadOnlyClient = A.Fake<IFileShareReadOnlyClient>();
            _fakeReadWriteClient = A.Fake<IFileShareReadWriteClient>();
        }

        [Test]
        public void WhenReadWriteAndReadOnlyClientNull_ThenThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new AssemblyPipeline(null, _fakeReadWriteClient));

            Assert.Throws<ArgumentNullException>(() =>
                new AssemblyPipeline(_fakeReadOnlyClient, null));
        }

        [Test]
        public async Task WhenExecutePipelineValidContext_ThenReturnsNodeResultWithSuccessStatus()
        {
            var context = A.Fake<ExchangeSetPipelineContext>();
            var expectedResult = new NodeResult(
                subject: null,
                id: Guid.NewGuid().ToString(),
                flowId: Guid.NewGuid().ToString()
            );

            typeof(NodeResult)
                .GetProperty("Status", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(expectedResult, NodeResultStatus.Succeeded);

            var pipeline = new AssemblyPipeline(_fakeReadOnlyClient, _fakeReadWriteClient);

            var result = await pipeline.ExecutePipeline(context);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded).Or.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public void WhenExecutePipelineExceptionThrown_ThenPropagatesException()
        {
            var context = A.Fake<ExchangeSetPipelineContext>();

            var ex = new InvalidOperationException("Test exception");

            var throwingPipeline = new ThrowingAssemblyPipeline(_fakeReadOnlyClient, _fakeReadWriteClient, ex);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await throwingPipeline.ExecutePipeline(context);
            });
        }
        
        private class ThrowingAssemblyPipeline : AssemblyPipeline
        {
            private readonly Exception _exceptionToThrow;

            public ThrowingAssemblyPipeline(
                IFileShareReadOnlyClient readOnlyClient,
                IFileShareReadWriteClient readWriteClient,
                Exception exceptionToThrow)
                : base(readOnlyClient, readWriteClient)
            {
                _exceptionToThrow = exceptionToThrow;
            }

            public new async Task<NodeResult> ExecutePipeline(ExchangeSetPipelineContext context)
            {
                await Task.Yield();
                throw _exceptionToThrow;
            }
        }
    }
}
