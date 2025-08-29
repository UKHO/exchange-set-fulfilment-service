using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NUnit.Framework.Internal;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Services;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Job = UKHO.ADDS.EFS.Domain.Jobs.Job;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Pipelines.Assembly.Nodes.S100
{
    [TestFixture]
    internal class GetS100ProductNamesNodeTest
    {
        private IOrchestratorSalesCatalogueClient _salesCatalogueClient;
        private GetS100ProductNamesNode _getS100ProductNamesNode;
        private IExecutionContext<PipelineContext<S100Build>> _executionContext;
        private AssemblyNodeEnvironment _nodeEnvironment;
        private ILogger<GetS100ProductNamesNode> _logger;
        private IConfiguration _configuration;
        private IStorageService _storageService;
        private Job _job;
        private S100Build _build;
        private PipelineContext<S100Build> _pipelineContext;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _salesCatalogueClient = A.Fake<IOrchestratorSalesCatalogueClient>();
            _logger = A.Fake<ILogger<GetS100ProductNamesNode>>();
            _configuration = A.Fake<IConfiguration>();
            _storageService = A.Fake<IStorageService>();
            _nodeEnvironment = new AssemblyNodeEnvironment(_configuration, CancellationToken.None, A.Fake<ILogger>());
            _getS100ProductNamesNode = new GetS100ProductNamesNode(_nodeEnvironment, _salesCatalogueClient, _logger);
            _executionContext = A.Fake<IExecutionContext<PipelineContext<S100Build>>>();
        }

        [SetUp]
        public void Setup()
        {
            var products = new ProductNameList();

            products.Add(ProductName.From("102CA005N5040W00130"));

            _job = new Job
            {
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = products,
                RequestedFilter = "",
            };

            _build = new S100Build
            {
                Products =
                [
                     new() { ProductName = ProductName.From("101GB004DEVQP"), LatestEditionNumber = EditionNumber.From(1), LatestUpdateNumber = UpdateNumber.From(0) },
                new() { ProductName = ProductName.From("101GB004DEVQK"), LatestEditionNumber = EditionNumber.From(2), LatestUpdateNumber = UpdateNumber.From(0) }
                ]
            };

            _pipelineContext = new PipelineContext<S100Build>(_job, _build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);
        }

        [Test]
        public void WhenSalesCatalogueClientIsNull_ThenThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GetS100ProductNamesNode(_nodeEnvironment, null!, _logger));
        }

        [Test]
        public void WhenLoggerIsNull_ThenThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GetS100ProductNamesNode(_nodeEnvironment, _salesCatalogueClient, null!));
        }

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledWithJobStateCreated_ThenReturnsTrue()
        {
            _job.ValidateAndSet(JobState.Created, BuildState.NotScheduled);

            var result = await _getS100ProductNamesNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledWithJobStateNotCreated_ThenReturnsFalse()
        {
            _job.ValidateAndSet(JobState.UpToDate, BuildState.NotScheduled);

            var result = await _getS100ProductNamesNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledWithRequestedProducts_ThenUsesRequestedProducts()
        {
            var salesCatalogueData = CreateSalesCatalogueData(HttpStatusCode.OK, 2, 0);
            A.CallTo(() => _salesCatalogueClient.GetS100ProductEditionListAsync(A<IEnumerable<ProductName>>._, _job, A<CancellationToken>._))
                .Returns(salesCatalogueData);

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));

            A.CallTo(() => _salesCatalogueClient.GetS100ProductEditionListAsync(
                A<IEnumerable<ProductName>>._, _job,
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

        }

        private static ProductEditionList CreateSalesCatalogueData(HttpStatusCode responseCode, int returnedCount, int missingCount)
        {
            var s100ProductEditionResponse = new ProductEditionList
            {
                ProductCountSummary = new ProductCountSummary
                {
                    RequestedProductCount = ProductCount.From(2),
                    ReturnedProductCount = ProductCount.From(2),
                    RequestedProductsAlreadyUpToDateCount = ProductCount.From(0),
                    MissingProducts = []
                },
                Products =
                [
                    new ProductEdition
                    {
                        ProductName = ProductName.From("101GB004DEVQK"),
                        //EditionNumber = EditionNumber.From(0),
                        //UpdateNumbers = [1,2]
                        //EditionStatus = "RELEASED",
                        //IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        //PlannedObsolescenceDate = null
                    },
                new ProductEdition
                {
                    ProductName = ProductName.From("101GB00510210"),
                    EditionNumber = EditionNumber.From(1),
                    UpdateNumbers = [1,2]
                    //EditionStatus = "RELEASED",
                    //IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    //PlannedObsolescenceDate = null
                }
                ],
                ResponseCode = HttpStatusCode.OK
            };

            return s100ProductEditionResponse;
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledWithEmptyRequestedProducts_ThenUsesProductsFromBuild()
        {
            _job = new Job
            {
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList(),
                RequestedFilter = "",
            };

            _pipelineContext = new PipelineContext<S100Build>(_job, _build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);

            var salesCatalogueData = CreateSalesCatalogueData(HttpStatusCode.OK, 2, 0);
            A.CallTo(() => _salesCatalogueClient.GetS100ProductEditionListAsync(A<IEnumerable<ProductName>>._, _job, A<CancellationToken>._))
                .Returns(salesCatalogueData);

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(_job.BuildState, Is.EqualTo(BuildState.NotScheduled));
            Assert.That(_build.ProductEditions, Is.EqualTo(salesCatalogueData.Products));

            A.CallTo(() => _salesCatalogueClient.GetS100ProductEditionListAsync(
                A<IEnumerable<ProductName>>._, _job,
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenSalesCatalogueReturnsOkButReturnedProductCountIsZero_ThenSignalsAssemblyErrorAndReturnsFailed()
        {
            var salesCatalogueData = CreateSalesCatalogueData(HttpStatusCode.OK, 2, 0);
            salesCatalogueData.ProductCountSummary.ReturnedProductCount = ProductCount.From(0);
            A.CallTo(() => _salesCatalogueClient.GetS100ProductEditionListAsync(A<IEnumerable<ProductName>>._, _job, A<CancellationToken>._))
                .Returns(salesCatalogueData);

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            Assert.That(_job.JobState, Is.EqualTo(JobState.Failed));
        }

        [Test]
        public async Task WhenSalesCatalogueReturnsOkButMissingProductCountIsGreaterThanZero_ThenSignalBuildRequiredd()
        {
            var salesCatalogueData = CreateSalesCatalogueData(HttpStatusCode.OK, 2, 0);
            salesCatalogueData.ProductCountSummary.MissingProducts = [new MissingProduct { ProductName = ProductName.From("101GB00510210") }];

            A.CallTo(() => _salesCatalogueClient.GetS100ProductEditionListAsync(A<IEnumerable<ProductName>>._, _job, A<CancellationToken>._))
                .Returns(salesCatalogueData);

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenSalesCatalogueReturnsBadRequest_ThenSignalsAssemblyErrorAndReturnsFailed()
        {
            var salesCatalogueData = CreateSalesCatalogueData(HttpStatusCode.OK, 2, 0);
            salesCatalogueData.ResponseCode = HttpStatusCode.BadRequest;

            A.CallTo(() => _salesCatalogueClient.GetS100ProductEditionListAsync(A<IEnumerable<ProductName>>._, _job, A<CancellationToken>._))
                .Returns(salesCatalogueData);

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            Assert.That(_job.JobState, Is.EqualTo(JobState.Failed));
        }        
    }
}
