using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
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
        private IExecutionContext<S100ExchangeSetPipelineContext> _executionContext;
        private ProductSearchNode _productSearchNode;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _fileShareReadOnlyClientFake = A.Fake<IFileShareReadOnlyClient>();
            _productSearchNode = new ProductSearchNode(_fileShareReadOnlyClientFake);
            _executionContext = A.Fake<IExecutionContext<S100ExchangeSetPipelineContext>>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            _logger = A.Fake<ILogger<ProductSearchNode>>();
        }

        [SetUp]
        public void Setup()
        {
            var exchangeSetPipelineContext = new S100ExchangeSetPipelineContext(null,  null, null, null, _loggerFactory)
            {
                Build = new S100Build
                {
                    JobId = JobId.From("TestCorrelationId"),
                    DataStandard = DataStandard.S100,
                    BatchId = BatchId.From("a-batch-id"),
                    ProductEditions =
                    [
                        new ProductEdition 
                        {
                            ProductName = ProductName.From("101TestProduct"),
                            EditionNumber = EditionNumber.From(1),
                            UpdateNumbers = [0, 1]
                        },
                        new ProductEdition 
                        {
                            ProductName = ProductName.From("101TestProduct2"),
                            EditionNumber = EditionNumber.From(2),
                            UpdateNumbers = [0, 1]
                        }
                    ]
                }
            };

            A.CallTo(() => _executionContext.Subject).Returns(exchangeSetPipelineContext);
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(ProductSearchNode).FullName!)).Returns(_logger);
        }

        [Test]
        public void WhenFileShareReadOnlyClientIsNull_ThenThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ProductSearchNode(null));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalledWithValidProducts_ThenReturnSucceeded()
        { 
            var batchDetails = new List<BatchDetails>
            {
                new() { BatchId = "TestBatchId1" }
            };
            A.CallTo(() => _fileShareReadOnlyClientFake.SearchAsync(A<string>._, A<int?>._, A<int?>._, A<string>._))
                .Returns(Result.Success(new BatchSearchResponse { Entries = batchDetails }));

            var result = await _productSearchNode.ExecuteAsync(_executionContext);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
                Assert.That(_executionContext.Subject.BatchDetails, Is.Not.Null);
                Assert.That(_executionContext.Subject.BatchDetails.ToList(), Has.Count.EqualTo(2));
                A.CallTo(() => _fileShareReadOnlyClientFake.SearchAsync(A<string>._, A<int?>._, A<int?>._, A<string>._))
               .MustHaveHappened();
            });
        }

        [Test]
        public async Task WhenPerformExecuteAsyncCalledWithNoProductsInContext_ThenReturnNoRun()
        {
            _executionContext.Subject.Build.Products = [];
            _executionContext.Subject.Build.ProductEditions = [];

            var result = await _productSearchNode.ExecuteAsync(_executionContext);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(NodeResultStatus.NotRun));
                Assert.That(_executionContext.Subject?.BatchDetails, Is.Null);
            });
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndSearchFails_ThenReturnFailed()
        {           
            var error = new Error { Message = "Search failed" };
            A.CallTo(() => _fileShareReadOnlyClientFake.SearchAsync(A<string>._, A<int?>._, A<int?>._, A<string>._))
                .Returns(Result.Failure<BatchSearchResponse>(error));
         
            var result = await _productSearchNode.ExecuteAsync(_executionContext);

            Assert.Multiple(() =>
            {
                Assert.That(_executionContext.Subject.BatchDetails, Is.Null);
                Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            });

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "ProductSearchNodeFssSearchFailed"),
                    A<LoggerMessageState>._,
                    A<Exception>._,
                    A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalled_ThenQueryIsCorrectlyConfigured()
        {
            var searchQuery = "BusinessUnit eq 'ADDS-S100' and $batch(ProductType) eq 'S-100' and (($batch(ProductName) eq '101TESTPRODUCT2' and $batch(EditionNumber) eq '2' and (($batch(UpdateNumber) eq '1' ))))";
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

            _executionContext.Subject.Build.ProductEditions = new List<ProductEdition>
            {
                new()
                {
                    ProductName = ProductName.From("101TestProduct2"),
                    EditionNumber = EditionNumber.From(2),
                    UpdateNumbers = new List<int> { 1 }
                }
            };

            await _productSearchNode.ExecuteAsync(_executionContext);

            Assert.That(capturedQuery, Is.Not.Null);
            Assert.That(capturedQuery, Is.EqualTo(searchQuery));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledFssReturnNextHrefUrl_ThenHandleNextHrefUrl()
        {
            var responses = new[]
            {
                new BatchSearchResponse
                {
                    Entries = [new BatchDetails { BatchId = "TestBatchId1" }],
                    Links = new Links(
                        self: null,
                        first: null,
                        previous: null,
                        next: new Link(href: "https://example.com?start=10&limit=5"),
                        last: null
                    )
                },
                new BatchSearchResponse
                {
                    Entries = [new BatchDetails { BatchId = "TestBatchId2" }],
                    Links = new Links(
                        self: null,
                        first: null,
                        previous: null,
                        next: new Link(href: "https://example.com?start=10&limit=5"),
                        last: null
                    )
                },
                new BatchSearchResponse
                {
                    Entries = [],
                    Links = new Links(
                        self: null,
                        first: null,
                        previous: null,
                        next: null,
                        last: null
                    )
                }
            };

            A.CallTo(() => _fileShareReadOnlyClientFake.SearchAsync(A<string>._, A<int?>._, A<int?>._, A<string>._))
                .ReturnsNextFromSequence(
                    Result.Success(responses[0]),
                    Result.Success(responses[1]),
                    Result.Success(responses[2])
                );

            var result = await _productSearchNode.ExecuteAsync(_executionContext);
            var batchDetails = _executionContext.Subject.BatchDetails?.ToList();

            Assert.Multiple(() =>
            {
                Assert.That(batchDetails, Is.Not.Null.And.Count.EqualTo(2));
                Assert.That(batchDetails!.Select(b => b.BatchId), Is.EquivalentTo(new[] { "TestBatchId1", "TestBatchId2" }));
            });
        }

        [Test]
        public async Task WhenPerformExecuteAsyncExceptionThrown_ThenLogErrorAndReturnFailed()
        {
            A.CallTo(() => _fileShareReadOnlyClientFake.SearchAsync(
                A<string>._,
                A<int?>._,
                A<int?>._,
                A<string>._))
                .Throws(new Exception("Test exception"));

            var result = await _productSearchNode.ExecuteAsync(_executionContext);

            A.CallTo(() => _logger.Log<LoggerMessageState>(
                    LogLevel.Error,
                    A<EventId>.That.Matches(e => e.Name == "ProductSearchNodeFailed"),
                    A<LoggerMessageState>._,
                    A<Exception>._,
                    A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappenedOnceExactly();
            
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _loggerFactory?.Dispose();
        }
    }
}
