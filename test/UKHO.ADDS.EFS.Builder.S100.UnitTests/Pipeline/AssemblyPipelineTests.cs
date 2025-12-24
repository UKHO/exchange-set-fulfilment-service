using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
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
    public class AssemblyPipelineTests
    {
        private IFileShareReadOnlyClient _fakeReadOnlyClient;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;
        private IExecutionContext<S100ExchangeSetPipelineContext> _executionContext;
        private S100ExchangeSetPipelineContext _pipelineContext;
        private IConfiguration _configuration;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _fakeReadOnlyClient = A.Fake<IFileShareReadOnlyClient>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            _logger = A.Fake<ILogger<AssemblyPipeline>>();
            _executionContext = A.Fake<IExecutionContext<S100ExchangeSetPipelineContext>>();
            _configuration = A.Fake<IConfiguration>();

            // Set up the S100ConcurrentDownloadLimitCount config value
            A.CallTo(() => _configuration["S100ConcurrentDownloadLimitCount"]).Returns("4");
        }

        [SetUp]
        public void SetUp()
        {
            var batchId = BatchId.From("a-batch-id");
            _pipelineContext = new S100ExchangeSetPipelineContext(null!, null!, null!, null!, _loggerFactory)
            {
                BatchId = batchId,
                Build = new S100Build
                {
                    JobId = JobId.From("TestCorrelationId"),
                    BatchId = batchId,
                    DataStandard = DataStandard.S100,
                    Products = GetProducts(),
                    ProductEditions = GetProductNames()
                },
            };

            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(AssemblyPipeline).FullName!)).Returns(_logger);
        }

        [Test]
        public void WhenReadOnlyClientNull_ThenThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AssemblyPipeline(null!, _configuration));
        }

        [Test]
        public void WhenConfigurationNull_ThenThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AssemblyPipeline(_fakeReadOnlyClient, null!));
        }

        [Test]
        public async Task WhenExecutePipelineValidContext_ThenReturnsNodeResultWithSuccessStatus()
        {
            var batchHandle = A.Fake<IBatchHandle>();
            A.CallTo(() => batchHandle.BatchId).Returns("ValidBatchId");
            A.CallTo(() => _fakeReadOnlyClient.SearchAsync(A<string>._, A<int?>._, A<int?>._, A<string>._))
                .Returns(Result.Success(new BatchSearchResponse { Entries = GetBatchDetails() }));

            var fakeResult = A.Fake<IResult<Stream>>();
            IError? outError = null;
            Stream outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream!)).Returns(false);

            A.CallTo(() => _fakeReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var pipeline = new AssemblyPipeline(_fakeReadOnlyClient, _configuration);

            var result = await pipeline.ExecutePipeline(_pipelineContext);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));

            A.CallTo(() => _fakeReadOnlyClient.SearchAsync(A<string>._, A<int?>._, A<int?>._, A<string>._))
                .MustHaveHappened();

            A.CallTo(() => _fakeReadOnlyClient.DownloadFileAsync(A<string>._, "file1.txt", A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .MustNotHaveHappened();
            A.CallTo(() => _fakeReadOnlyClient.DownloadFileAsync(A<string>._, "ABC1234.001", A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeReadOnlyClient.DownloadFileAsync(A<string>._, "DEF5678.h5", A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenExecutePipelineExceptionThrown_ThenPropagatesException()
        {
            var context = A.Fake<S100ExchangeSetPipelineContext>();

            var ex = new InvalidOperationException("Test exception");

            var throwingPipeline = new ThrowingAssemblyPipeline(_fakeReadOnlyClient, _configuration, ex);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await throwingPipeline.ExecutePipeline(context);
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _loggerFactory?.Dispose();
        }

        private static List<BatchDetails> GetBatchDetails()
        {
            return
            [
                new BatchDetails{
                    BatchId= "TestBatchId",
                    Attributes=
                    [
                        new BatchDetailsAttributes("ProductName", ""),
                        new BatchDetailsAttributes("EditionNumber", "1"),
                        new BatchDetailsAttributes("UpdateNumber", "0")
                        ],
                    BatchPublishedDate= DateTime.UtcNow,
                    Files=
                    [
                        new BatchDetailsFiles("file1.txt", 0L),
                        new BatchDetailsFiles("ABC1234.001", 1L),
                        new BatchDetailsFiles("DEF5678.h5", 2L)
                        ]
                }
                ];
        }

        private static List<Product> GetProducts()
        {
            return [
                new Product
                {
                    ProductName = ProductName.From("101TestProduct"),
                    LatestEditionNumber = EditionNumber.From(1),
                    LatestUpdateNumber = UpdateNumber.From(0),
                    Status=new ProductStatus
                    {
                        StatusName="newDataSet",
                        StatusDate=DateTime.UtcNow.AddDays(-7)
                    }
                }
                ];
        }

        private static List<ProductEdition> GetProductNames()
        {
            return [
                new ProductEdition
                {
                    ProductName = ProductName.From("101TestProduct"),
                    EditionNumber = EditionNumber.From(1),
                    UpdateNumbers = [UpdateNumber.From(0), UpdateNumber.From(1)]
                }
            ];
        }

        private class ThrowingAssemblyPipeline(IFileShareReadOnlyClient readOnlyClient, IConfiguration configuration, Exception exceptionToThrow) : AssemblyPipeline(readOnlyClient, configuration)
        {
            private readonly Exception _exceptionToThrow = exceptionToThrow;

            public new async Task<NodeResult> ExecutePipeline(S100ExchangeSetPipelineContext _)
            {
                await Task.Yield();
                throw _exceptionToThrow;
            }
        }
    }
}
