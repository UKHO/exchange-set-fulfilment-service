using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Services;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

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
            _job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,               
                RequestedProducts = new ProductNameList([new ProductName("102CA005N5040W00130")]),
                RequestedFilter = "",
            };

            _build = new S100Build
            {
                Products =
                [
                    new() { ProductName = "101GB004DEVQK",LatestEditionNumber = 1 },
                    new() { ProductName = "101GB00510210", LatestEditionNumber = 2 }
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
            var requestedProducts = new[] { "101GB004DEVQK", "101GB00510210" };
            _job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = requestedProducts,
                RequestedFilter = "",
            };

            _pipelineContext = new PipelineContext<S100Build>(_job, _build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);

            var s100ProductNamesResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.OK,
                Products = new List<S100ProductNames> { new S100ProductNames { ProductName = "101GB004DEVQK" } },
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 2,
                    ReturnedProductCount = 1,
                    RequestedProductsAlreadyUpToDateCount = 0,
                    RequestedProductsNotReturned = []
                }
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                A<IEnumerable<string>>.That.IsSameSequenceAs(requestedProducts),
                _job,
                A<CancellationToken>._))
                .Returns(Task.FromResult(s100ProductNamesResponse));

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                A<IEnumerable<string>>.That.IsSameSequenceAs(requestedProducts),
                _job,
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledWithEmptyRequestedProducts_ThenUsesProductsFromBuild()
        {
            _job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = [],
                RequestedFilter = "",
            };

            var expectedProductNames = new[] { "101GB004DEVQK", "101GB00510210" };

            _pipelineContext = new PipelineContext<S100Build>(_job, _build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);

            var s100ProductNamesResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.OK,
                Products = new List<S100ProductNames> { new() { ProductName = "101GB004DEVQK,101GB00510210" } },
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 2,
                    ReturnedProductCount = 1,
                    RequestedProductsAlreadyUpToDateCount = 0,
                    RequestedProductsNotReturned = []
                }
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                A<IEnumerable<string>>.That.IsSameSequenceAs(expectedProductNames),
                _job,
                A<CancellationToken>._))
                .Returns(Task.FromResult(s100ProductNamesResponse));

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                A<IEnumerable<string>>.That.IsSameSequenceAs(expectedProductNames),
                _job,
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledWithNullBuildProducts_ThenUsesEmptyProductNames()
        {
            _job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = [],
                RequestedFilter = "",
            };
            _build = new S100Build
            {
                Products = null
            };

            _pipelineContext = new PipelineContext<S100Build>(_job, _build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);

            var s100ProductNamesResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.OK,
                Products = new List<S100ProductNames>(),
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 0,
                    ReturnedProductCount = 0,
                    RequestedProductsAlreadyUpToDateCount = 0,
                    RequestedProductsNotReturned = []
                }
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                A<IEnumerable<string>>.That.IsEmpty(),
                _job,
                A<CancellationToken>._))
                .Returns(Task.FromResult(s100ProductNamesResponse));

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            Assert.That(_job.JobState, Is.EqualTo(JobState.Failed));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledWithProductsContainingNullOrWhiteSpaceNames_ThenFiltersOutInvalidNames()
        {
            _job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = [],
                RequestedFilter = "",
            };

            _build = new S100Build
            {
                Products =
                [
                    new() { ProductName = "102CA005N5040W00130", LatestEditionNumber = 1 },
                    new() { ProductName = "", LatestEditionNumber = 2 },
                    new() { ProductName = "   ", LatestEditionNumber = 3 },
                    new() { ProductName = null!, LatestEditionNumber = 4 },
                    new() { ProductName = "101GB004DEVQK", LatestEditionNumber = 5 }
                ]
            };

            var expectedProductNames = new[] { "102CA005N5040W00130", "101GB004DEVQK" };

            _pipelineContext = new PipelineContext<S100Build>(_job, _build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);

            var s100ProductNamesResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.OK,
                Products = new List<S100ProductNames> { new S100ProductNames { ProductName = "102CA005N5040W00130" } },
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 2,
                    ReturnedProductCount = 1,
                    RequestedProductsAlreadyUpToDateCount = 0,
                    RequestedProductsNotReturned = []
                }
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                A<IEnumerable<string>>.That.IsSameSequenceAs(expectedProductNames),
                _job,
                A<CancellationToken>._))
                .Returns(Task.FromResult(s100ProductNamesResponse));

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                A<IEnumerable<string>>.That.IsSameSequenceAs(expectedProductNames),
                _job,
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenSalesCatalogueReturnsOkWithProducts_ThenSetsProductNamesAndSignalsBuildRequired()
        {
            _job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = [],
                RequestedFilter = "",
            };

            var productNames = new List<S100ProductNames>
            {
                new() { ProductName = "101GB004DEVQK", EditionNumber = 1 },
                new() { ProductName = "101GB00510210", EditionNumber = 2 }
            };

            var s100ProductNamesResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.OK,
                Products = productNames,
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 2,
                    ReturnedProductCount = 2,
                    RequestedProductsAlreadyUpToDateCount = 0,
                    RequestedProductsNotReturned = []
                }
            };

            _pipelineContext = new PipelineContext<S100Build>(_job, _build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                A<IEnumerable<string>>._,
                _job,
                A<CancellationToken>._))
                .Returns(Task.FromResult(s100ProductNamesResponse));

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(_build.ProductNames, Is.EqualTo(productNames));
        }

        [Test]
        public async Task WhenSalesCatalogueReturnsOkButReturnedProductCountIsZero_ThenSignalsAssemblyErrorAndReturnsFailed()
        {
            var s100ProductNamesResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.OK,
                Products = new List<S100ProductNames> { new S100ProductNames { ProductName = "101GB004DEVQK" } },
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 2,
                    ReturnedProductCount = 0,
                    RequestedProductsAlreadyUpToDateCount = 0,
                    RequestedProductsNotReturned = []
                }
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                A<IEnumerable<string>>._,
                _job,
                A<CancellationToken>._))
                .Returns(Task.FromResult(s100ProductNamesResponse));

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            Assert.That(_job.JobState, Is.EqualTo(JobState.Failed));
        }

        [Test]
        public async Task WhenSalesCatalogueReturnsOkWithSomeProductsNotReturned_ThenLogsWarningButSucceeds()
        {
            var productNames = new List<S100ProductNames>
            {
                new() { ProductName = "Product1", EditionNumber = 1 }
            };

            var requestedProductsNotReturned = new List<RequestedProductsNotReturned>
            {
                new() { ProductName = "Product2", Reason = "Product not found" }
            };

            var s100ProductNamesResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.OK,
                Products = productNames,
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 2,
                    ReturnedProductCount = 1,
                    RequestedProductsAlreadyUpToDateCount = 0,
                    RequestedProductsNotReturned = requestedProductsNotReturned
                }
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                A<IEnumerable<string>>._,
                _job,
                A<CancellationToken>._))
                .Returns(Task.FromResult(s100ProductNamesResponse));

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(_build.ProductNames, Is.EqualTo(productNames));
        }

        [Test]
        public async Task WhenSalesCatalogueReturnsOkButNoProductsFound_ThenSignalsAssemblyErrorAndReturnsFailed()
        {
            var s100ProductNamesResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.OK,
                Products = new List<S100ProductNames>(),
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 2,
                    ReturnedProductCount = 0,
                    RequestedProductsAlreadyUpToDateCount = 0,
                    RequestedProductsNotReturned = []
                }
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                A<IEnumerable<string>>._,
                _job,
                A<CancellationToken>._))
                .Returns(Task.FromResult(s100ProductNamesResponse));

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenSalesCatalogueReturnsBadRequest_ThenSignalsAssemblyErrorAndReturnsFailed()
        {
            var s100ProductNamesResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.BadRequest,
                Products = new List<S100ProductNames>(),
                ProductCounts = new ProductCounts()
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                A<IEnumerable<string>>._,
                _job,
                A<CancellationToken>._))
                .Returns(Task.FromResult(s100ProductNamesResponse));

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenSalesCatalogueReturnsInternalServerError_ThenSignalsAssemblyErrorAndReturnsFailed()
        {
            var s100ProductNamesResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.InternalServerError,
                Products = new List<S100ProductNames>(),
                ProductCounts = new ProductCounts()
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                A<IEnumerable<string>>._,
                _job,
                A<CancellationToken>._))
                .Returns(Task.FromResult(s100ProductNamesResponse));

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalled_ThenUsesCancellationTokenFromEnvironment()
        {
            var s100ProductNamesResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.OK,
                Products = new List<S100ProductNames> { new S100ProductNames { ProductName = "Product1" } },
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 1,
                    ReturnedProductCount = 1,
                    RequestedProductsAlreadyUpToDateCount = 0,
                    RequestedProductsNotReturned = []
                }
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                A<IEnumerable<string>>._,
                _job,
                _nodeEnvironment.CancellationToken))
                .Returns(Task.FromResult(s100ProductNamesResponse));

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                A<IEnumerable<string>>._,
                _job,
                _nodeEnvironment.CancellationToken))
                .MustHaveHappenedOnceExactly();
        }
    }
}
