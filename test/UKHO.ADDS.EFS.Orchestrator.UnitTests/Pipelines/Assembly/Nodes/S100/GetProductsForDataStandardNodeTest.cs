using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Services;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
//using UKHO.ADDS.Infrastructure.Storage;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Pipelines.Assembly.Nodes.S100
{
    [TestFixture]
    internal class GetProductsForDataStandardNodeTest
    {
        private IOrchestratorSalesCatalogueClient _salesCatalogueClient;
        private GetProductsForDataStandardNode _getProductsForDataStandardNode;
        private IExecutionContext<PipelineContext<S100Build>> _executionContext;
        private AssemblyNodeEnvironment _nodeEnvironment;
        private IConfiguration _configuration;
        private ILogger _logger;
        private IStorageService _storageService;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _salesCatalogueClient = A.Fake<IOrchestratorSalesCatalogueClient>();
            _configuration = A.Fake<IConfiguration>();
            _logger = A.Fake<ILogger>();
            _storageService = A.Fake<IStorageService>();
            _nodeEnvironment = new AssemblyNodeEnvironment(_configuration, CancellationToken.None, _logger);
            _getProductsForDataStandardNode = new GetProductsForDataStandardNode(_nodeEnvironment, _salesCatalogueClient);
            _executionContext = A.Fake<IExecutionContext<PipelineContext<S100Build>>>();
        }

        [SetUp]
        public void Setup()
        {
            //A.CallTo(_salesCatalogueClient).CallsTo(x => x.GetS100ProductsFromSpecificDateAsync(A<DateTime?>._, A<Job>._))
            //    .Returns(Task.FromResult((
            //        new S100SalesCatalogueResponse { ResponseCode = HttpStatusCode.OK, ResponseBody = [] },
            //        (DateTime?)DateTime.UtcNow
            //    )));
        }

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledWithJobStateCreatedAndEmptyRequestedProducts_ThenReturnsTrue()
        {
            var job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = [],
                RequestedFilter = ""
            };
            job.ValidateAndSet(JobState.Created, BuildState.NotScheduled);

            var build = new S100Build();
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var result = await _getProductsForDataStandardNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledWithJobStateCreatedAndNullRequestedProducts_ThenReturnsTrue()
        {
            var job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = null!,
                RequestedFilter = ""
            };
            job.ValidateAndSet(JobState.Created, BuildState.NotScheduled);

            var build = new S100Build();
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var result = await _getProductsForDataStandardNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledWithJobStateCreatedAndNonEmptyRequestedProducts_ThenReturnsFalse()
        {
            var job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = ["101GB004DEVQK", "101GB00510210"],
                RequestedFilter = ""
            };
            job.ValidateAndSet(JobState.Created, BuildState.NotScheduled);

            var build = new S100Build();
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var result = await _getProductsForDataStandardNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledWithJobStateNotCreated_ThenReturnsFalse()
        {
            var job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = [],
                RequestedFilter = ""
            };
            job.ValidateAndSet(JobState.Submitted, BuildState.NotScheduled);

            var build = new S100Build();
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var result = await _getProductsForDataStandardNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndSalesCatalogueReturnsOKWithProducts_ThenReturnsSucceededAndSignalsBuildRequired()
        {
            var job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = [],
                RequestedFilter = "",
                DataStandardTimestamp = DateTime.UtcNow.AddDays(-1)
            };

            var build = new S100Build();
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var products = new List<S100Products>
            {
                new() { ProductName = "101GB004DEVQK", LatestEditionNumber = 1 },
                new() { ProductName = "101GB004DEVQP", LatestEditionNumber = 2 }
            };

            var lastModified = DateTime.UtcNow;
            var salesCatalogueResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody = products
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(job.DataStandardTimestamp, job))
                .Returns(Task.FromResult((salesCatalogueResponse, (DateTime?)lastModified)));

            var result = await _getProductsForDataStandardNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(build.Products, Is.EqualTo(products));
            Assert.That(job.DataStandardTimestamp, Is.EqualTo(lastModified));
            Assert.That(build.SalesCatalogueTimestamp, Is.EqualTo(lastModified));
            Assert.That(job.BuildState, Is.EqualTo(BuildState.NotScheduled));//To check SignalBuildRequired indirectly
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndSalesCatalogueReturnsOKWithEmptyProducts_ThenReturnsFailed()
        {
            var job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = [],
                RequestedFilter = "",
                DataStandardTimestamp = DateTime.UtcNow.AddDays(-1)
            };

            var build = new S100Build();
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var salesCatalogueResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody = []
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(job.DataStandardTimestamp, job))
                .Returns(Task.FromResult((salesCatalogueResponse, (DateTime?)DateTime.UtcNow)));

            var result = await _getProductsForDataStandardNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndSalesCatalogueReturnsNotModified_ThenReturnsSucceededAndSignalsNoBuildRequired()
        {
            var job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = [],
                RequestedFilter = "",
                DataStandardTimestamp = DateTime.UtcNow.AddDays(-1)
            };

            var build = new S100Build();
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var lastModified = DateTime.UtcNow;
            var salesCatalogueResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.NotModified,
                ResponseBody = []
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(job.DataStandardTimestamp, job))
                .Returns(Task.FromResult((salesCatalogueResponse, (DateTime?)lastModified)));

            var result = await _getProductsForDataStandardNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(job.DataStandardTimestamp, Is.EqualTo(lastModified));
            Assert.That(job.BuildState, Is.EqualTo(BuildState.None));
            Assert.That(job.JobState, Is.EqualTo(JobState.UpToDate));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndSalesCatalogueReturnsInternalServerError_ThenReturnsFailedAndSignalsAssemblyError()
        {
            var job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = [],
                RequestedFilter = "",
                DataStandardTimestamp = DateTime.UtcNow.AddDays(-1)
            };

            var build = new S100Build();
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var salesCatalogueResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.InternalServerError,
                ResponseBody = []
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(job.DataStandardTimestamp, job))
                .Returns(Task.FromResult((salesCatalogueResponse, (DateTime?)DateTime.UtcNow)));

            var result = await _getProductsForDataStandardNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            Assert.That(job.BuildState, Is.EqualTo(BuildState.None));
            Assert.That(job.JobState, Is.EqualTo(JobState.Failed));

        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndSalesCatalogueReturnsBadRequest_ThenReturnsFailedAndSignalsAssemblyError()
        {
            var job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = [],
                RequestedFilter = "",
                DataStandardTimestamp = DateTime.UtcNow.AddDays(-1)
            };

            var build = new S100Build();
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var salesCatalogueResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.BadRequest,
                ResponseBody = []
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(job.DataStandardTimestamp, job))
                .Returns(Task.FromResult((salesCatalogueResponse, (DateTime?)DateTime.UtcNow)));

            var result = await _getProductsForDataStandardNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            Assert.That(job.BuildState, Is.EqualTo(BuildState.None));
            Assert.That(job.JobState, Is.EqualTo(JobState.Failed));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledWithNullDataStandardTimestamp_ThenCallsSalesCatalogueWithNullTimestamp()
        {
            var job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = [],
                RequestedFilter = "",
                DataStandardTimestamp = null
            };

            var build = new S100Build();
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var products = new List<S100Products>
            {
                new() { ProductName = "101GB004DEVQK", LatestEditionNumber = 1 }
            };

            var salesCatalogueResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody = products
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(null, job))
                .Returns(Task.FromResult((salesCatalogueResponse, (DateTime?)DateTime.UtcNow)));

            var result = await _getProductsForDataStandardNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));

            A.CallTo(() => _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(null, job))
                .MustHaveHappenedOnceExactly();
        }
    }
}
