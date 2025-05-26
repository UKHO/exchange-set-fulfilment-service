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
using Error = UKHO.ADDS.Infrastructure.Results.Error;

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
                    Products =
                    [
                        new S100Products { ProductName = "Product1", LatestEditionNumber = 1, LatestUpdateNumber = 0 },
                        new S100Products { ProductName = "Product2", LatestEditionNumber = 2, LatestUpdateNumber = 1 }
                    ]
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
        public async Task WhenPerformExecuteAsyncCalledWithValidProducts_ThenReturnSucceeded()
        {
            // Arrange  
            var batchDetails = new List<BatchDetails>
            {
                new() { BatchId = "TestBatchId1" }
            };
            A.CallTo(() => _fileShareReadOnlyClientFake.SearchAsync(A<string>._, A<int?>._, A<int?>._, A<string>._))
                .Returns(Result.Success(new BatchSearchResponse { Entries = batchDetails }));

            // Act
            var result = await _testableProductSearchNode.PerformExecuteAsync(_executionContext);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(result, Is.EqualTo(NodeResultStatus.Succeeded));
                Assert.That(_executionContext.Subject.BatchDetails, Is.Not.Null);
                Assert.That(_executionContext.Subject.BatchDetails.ToList(), Has.Count.EqualTo(2));
                A.CallTo(() => _fileShareReadOnlyClientFake.SearchAsync(A<string>._, A<int?>._, A<int?>._, A<string>._))
               .MustHaveHappened();
            });
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalledWithNoProductsInContext_ThenReturnNoRun()
        {
            // Arrange
            _executionContext.Subject.Job.Products = [];

            // Act
            var result = await _testableProductSearchNode.PerformExecuteAsync(_executionContext);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(result, Is.EqualTo(NodeResultStatus.NotRun));
                Assert.That(_executionContext.Subject?.BatchDetails, Is.Null);
            });
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndSearchFails_ThenReturnFailed()
        {
            // Arrange            
            var error = new Error { Message = "Search failed" };
            A.CallTo(() => _fileShareReadOnlyClientFake.SearchAsync(A<string>._, A<int?>._, A<int?>._, A<string>._))
                .Returns(Result.Failure<BatchSearchResponse>(error));

            // Act            
            var result = await _testableProductSearchNode.PerformExecuteAsync(_executionContext);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_executionContext.Subject.BatchDetails, Is.Null);
                Assert.That(result, Is.EqualTo(NodeResultStatus.Failed));
            });
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalled_ThenQueryIsCorrectlyConfigured()
        {
            // Arrange
            var searchQuery = "BusinessUnit eq 'ADDS-S100' and $batch(ProductType) eq 'S-100' and (($batch(ProductName) eq 'Product2' and $batch(EditionNumber) eq '2' and (($batch(UpdateNumber) eq '1' ))))";
            string? capturedQuery = null;
            var batchResponse = new BatchSearchResponse
            {
                Entries = []
            };
            A.CallTo(() => _fileShareReadOnlyClientFake.SearchAsync(A<string>._, A<int?>._, A<int?>._, A<string>._))
                .Invokes((string query, int? pageSize, int? start, string correlationId) =>
                {
                    capturedQuery = query;
                })
                .Returns(Result.Success(batchResponse));

            // Act
            await _testableProductSearchNode.PerformExecuteAsync(_executionContext);

            // Assert
            Assert.That(capturedQuery, Is.Not.Null);
            Assert.That(capturedQuery, Is.EqualTo(searchQuery));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledFssReturnNextHrefUrl_ThenHandleNextHrefUrl()
        {
            // Arrange
            var batchSearchResponseOne = new BatchSearchResponse
            {
                Entries = [new BatchDetails { BatchId = "TestBatchId1" }],
                Links = new Links(
                    self: null,
                    first: null,
                    previous: null,
                    next: new Link(href: "https://example.com?start=10&limit=5"),
                    last: null
                )
            };

            var batchSearchResponseTwo = new BatchSearchResponse
            {
                Entries = [new BatchDetails { BatchId = "TestBatchId2" }],
            };
            A.CallTo(() => _fileShareReadOnlyClientFake.SearchAsync(A<string>._, A<int?>._, A<int?>._, A<string>._))
                .ReturnsNextFromSequence(
                Result.Success(batchSearchResponseOne),
                Result.Success(batchSearchResponseTwo)
                );

            // Act
            var result = await _testableProductSearchNode.PerformExecuteAsync(_executionContext);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_executionContext.Subject.BatchDetails.ToList(), Has.Count.EqualTo(2));
                Assert.That(_executionContext.Subject.BatchDetails.ToList()[0].BatchId, Is.EqualTo("TestBatchId1"));
                Assert.That(_executionContext.Subject.BatchDetails.ToList()[1].BatchId, Is.EqualTo("TestBatchId2"));
            });
        }
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
