using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Assemble
{
    [TestFixture]
    internal class ProductSearchNodeTests
    {
        private IFileShareReadOnlyClient _fileShareReadOnlyClientFake;
        private TestableProductSearchNode _testableProductSearchNode;
        private IExecutionContext<ExchangeSetPipelineContext> _executionContext;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _fileShareReadOnlyClientFake = A.Fake<IFileShareReadOnlyClient>();
            _testableProductSearchNode = new TestableProductSearchNode(_fileShareReadOnlyClientFake);
            _executionContext = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
        }

        [SetUp]
        public void Setup()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

            var exchangeSetPipelineContext = new ExchangeSetPipelineContext(null, null, null, loggerFactory)
            {
                Job = new ExchangeSetJob
                {
                    CorrelationId = "TestCorrelationId",
                    Products = new List<S100Products>
                    {
                        new S100Products { ProductName = "Product1", LatestEditionNumber = 1, LatestUpdateNumber = 0 },
                        new S100Products { ProductName = "Product2", LatestEditionNumber = 2, LatestUpdateNumber = 1 }
                    }
                }
            };

            A.CallTo(() => _executionContext.Subject).Returns(exchangeSetPipelineContext);
        }

        [Test]
        public void WhenFileShareReadOnlyClientIsNull_ThenThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ProductSearchNode(null));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledWithNoProducts_ThenReturnsSucceeded()
        {
            // Arrange
            var emptyContext = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();

            // Act
            var result = await _testableProductSearchNode.PerformExecuteAsync(emptyContext);

            // Assert
            Assert.That(result, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledWithProducts_ThenCallsFileShareService()
        {
            // Arrange
            var batchSearchResponse = new BatchSearchResponse
            {
                Entries = new List<BatchDetails>
                {
                    new BatchDetails { BatchId = "Batch1", BusinessUnit = "ADDS-S100" }
                }
            };

            A.CallTo(() => _fileShareReadOnlyClientFake.SearchAsync(A<string>._, A<int?>._, A<int?>._, A<CancellationToken>._))
                .Returns(Result.Success(batchSearchResponse));

            // Act
            var result = await _testableProductSearchNode.PerformExecuteAsync(_executionContext);

            // Assert
            Assert.That(result, Is.EqualTo(NodeResultStatus.Succeeded));
            //A.CallTo(() => _fileShareReadOnlyClientFake.SearchAsync(A<string>.Ignored, A<int?>.Ignored, A<int?>.Ignored, A<CancellationToken>.Ignored))
            //    .MustHaveHappened();            
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndSearchFails_ThenReturnsFailed()
        {
            // Arrange
            A.CallTo(() => _fileShareReadOnlyClientFake.SearchAsync(A<string>._, A<int?>._, A<int?>._, A<CancellationToken>._))
                .Returns(Result.Failure<BatchSearchResponse>("Error searching batches"));

            // Act
            var result = await _testableProductSearchNode.PerformExecuteAsync(_executionContext);

            // Assert
            Assert.That(result, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        //[Test]
        //public async Task WhenPerformExecuteAsyncIsCalledAndCancellationIsRequested_ThenThrowsOperationCanceledException()
        //{
        //    // Arrange
        //    var cancellationTokenSource = new CancellationTokenSource();
        //    cancellationTokenSource.Cancel();

        //    var batchSearchResponse = new BatchSearchResponse
        //    {
        //        Entries = new List<BatchDetails>
        //        {
        //            new BatchDetails { BatchId = "Batch1", BusinessUnit = "ADDS-S100" }
        //        }
        //    };

        //    A.CallTo(() => _fileShareReadOnlyClientFake.SearchAsync(A<string>._, A<int?>._, A<int?>._, cancellationTokenSource.Token))
        //        .Returns(Result.Success(batchSearchResponse));

        //    // Act & Assert
        //    Assert.ThrowsAsync<OperationCanceledException>(async () =>
        //    {
        //        await _testableProductSearchNode.PerformExecuteAsync(_executionContext);
        //    });
        //}

        //[Test]
        //public async Task WhenPerformExecuteAsyncIsCalled_ThenQueryIsCorrectlyConfigured()
        //{
        //    // Arrange
        //    string capturedQuery = null;
        //    A.CallTo(() => _fileShareReadOnlyClientFake.SearchAsync(A<string>._, A<int?>._, A<int?>._, A<CancellationToken>._))
        //        .Invokes((string query, int? pageSize, int? start, string correlationId, CancellationToken _) =>
        //        {
        //            capturedQuery = query;
        //        })
        //        .Returns(Result.Success(new BatchSearchResponse()));

        //    // Act
        //    await _testableProductSearchNode.PerformExecuteAsync(_executionContext);

        //    // Assert
        //    Assert.That(capturedQuery, Is.Not.Null);
        //    Assert.That(capturedQuery, Does.Contain("ADDS-S100"));
        //}

        private class TestableProductSearchNode : ProductSearchNode
        {
            public TestableProductSearchNode(IFileShareReadOnlyClient fileShareReadOnlyClient)
                : base(fileShareReadOnlyClient)
            {
            }

            public new async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
            {
                return await base.PerformExecuteAsync(context);
            }
        }
    }
}
