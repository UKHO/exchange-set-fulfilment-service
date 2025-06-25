using FakeItEasy;
using Microsoft.Extensions.Configuration;
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
        private IExecutionContext<ExchangeSetPipelineContext> _executionContext;
        private ProductSearchNode _productSearchNode;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;
        private IConfiguration _configuration;

        private const int TestRetryDelayMs = 500;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _fileShareReadOnlyClientFake = A.Fake<IFileShareReadOnlyClient>();
            _productSearchNode = new ProductSearchNode(_fileShareReadOnlyClientFake);
            _executionContext = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            _logger = A.Fake<ILogger<ProductSearchNode>>();
        }

        [SetUp]
        public void Setup()
        {
            var exchangeSetPipelineContext = new ExchangeSetPipelineContext(null, null, null, _loggerFactory)
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
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(ProductSearchNode).FullName!)).Returns(_logger);

            _configuration = A.Fake<IConfiguration>();
            A.CallTo(() => _configuration["HttpRetry:RetryDelayInMilliseconds"]).Returns(TestRetryDelayMs.ToString());
            UKHO.ADDS.EFS.RetryPolicy.HttpRetryPolicyFactory.SetConfiguration(_configuration);
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
            _executionContext.Subject.Job.Products = [];

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

            await _productSearchNode.ExecuteAsync(_executionContext);

            Assert.That(capturedQuery, Is.Not.Null);
            Assert.That(capturedQuery, Is.EqualTo(searchQuery));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledFssReturnNextHrefUrl_ThenHandleNextHrefUrl()
        {
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

            var result = await _productSearchNode.ExecuteAsync(_executionContext);

            Assert.Multiple(() =>
            {
                Assert.That(_executionContext.Subject.BatchDetails.ToList(), Has.Count.EqualTo(2));
                Assert.That(_executionContext.Subject.BatchDetails.ToList()[0].BatchId, Is.EqualTo("TestBatchId1"));
                Assert.That(_executionContext.Subject.BatchDetails.ToList()[1].BatchId, Is.EqualTo("TestBatchId2"));
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

        [TearDown]
        public void TearDown()
        {
            UKHO.ADDS.EFS.RetryPolicy.HttpRetryPolicyFactory.SetConfiguration(null);
        }
    }
}
