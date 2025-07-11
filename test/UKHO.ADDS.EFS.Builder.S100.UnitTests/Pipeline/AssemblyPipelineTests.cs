using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Jobs;
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
            _pipelineContext = new S100ExchangeSetPipelineContext(null, null, null, null, _loggerFactory)
            {
                Build = new S100Build
                {
                    JobId = "TestCorrelationId",
                    BatchId = "a-batch-id",
                    DataStandard = DataStandard.S100,
                    Products = GetProducts()
                },
            };

            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);
            A.CallTo(() => _loggerFactory.CreateLogger(typeof(AssemblyPipeline).FullName!)).Returns(_logger);
        }

        [Test]
        public void WhenReadOnlyClientNull_ThenThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new AssemblyPipeline(null, _configuration));
        }

        [Test]
        public void WhenConfigurationNull_ThenThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new AssemblyPipeline(_fakeReadOnlyClient, null));
        }

        [Test]
        public async Task WhenExecutePipelineValidContext_ThenReturnsNodeResultWithSuccessStatus()
        {
            var batchHandle = A.Fake<IBatchHandle>();
            A.CallTo(() => batchHandle.BatchId).Returns("ValidBatchId");
            A.CallTo(() => _fakeReadOnlyClient.SearchAsync(A<string>._, A<int?>._, A<int?>._, A<string>._))
                .Returns(Result.Success(new BatchSearchResponse { Entries = GetBatchDetails() }));

            var fakeResult = A.Fake<IResult<Stream>>();
            IError outError = null;
            Stream outStream = new MemoryStream();
            A.CallTo(() => fakeResult.IsFailure(out outError, out outStream)).Returns(false);

            A.CallTo(() => _fakeReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .Returns(Task.FromResult(fakeResult));

            var pipeline = new AssemblyPipeline(_fakeReadOnlyClient, _configuration);

            var result = await pipeline.ExecutePipeline(_pipelineContext);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));

            A.CallTo(() => _fakeReadOnlyClient.SearchAsync(A<string>._, A<int?>._, A<int?>._, A<string>._))
                .MustHaveHappened();

            A.CallTo(() => _fakeReadOnlyClient.DownloadFileAsync(A<string>._, A<string>._, A<Stream>._, A<string>._, A<long>._, A<CancellationToken>._))
                .MustHaveHappened();
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
                        new BatchDetailsFiles("file1.txt"),
                        new BatchDetailsFiles("ABC1234.001"),
                        new BatchDetailsFiles("DEF5678.h5")
                        ]
                }
                ];
        }

        private List<S100Products> GetProducts()
        {
            return [
                new S100Products
                {
                    ProductName="TestProduct",
                    LatestEditionNumber=1,
                    LatestUpdateNumber=0,
                    Status=new S100ProductStatus
                    {
                        StatusName="newDataSet",
                        StatusDate=DateTime.UtcNow.AddDays(-7)
                    }
                }
                ];
        }

        private class ThrowingAssemblyPipeline : AssemblyPipeline
        {
            private readonly Exception _exceptionToThrow;

            public ThrowingAssemblyPipeline(
                IFileShareReadOnlyClient readOnlyClient,
                IConfiguration configuration,
                Exception exceptionToThrow)
                : base(readOnlyClient, configuration)
            {
                _exceptionToThrow = exceptionToThrow;
            }

            public new async Task<NodeResult> ExecutePipeline(S100ExchangeSetPipelineContext context)
            {
                await Task.Yield();
                throw _exceptionToThrow;
            }
        }
    }
}
