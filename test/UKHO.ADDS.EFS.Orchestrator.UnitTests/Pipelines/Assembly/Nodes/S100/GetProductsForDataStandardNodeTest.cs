using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledWithJobStateCreatedAndEmptyRequestedProducts_ThenReturnsTrue()
        {
            var job = new Job
            {
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList(),
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
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList(),
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
            var products = new ProductNameList();

            products.Add(ProductName.From("101GB004DEVQK"));
            products.Add(ProductName.From("101GB00510210"));

            var job = new Job
            {
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = products,
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
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList(),
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
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList(),
                RequestedFilter = "",
                DataStandardTimestamp = DateTime.UtcNow.AddDays(-1)
            };

            var build = new S100Build();
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var products = new List<Product>
            {
                new() { ProductName = ProductName.From("101GB004DEVQP"), LatestEditionNumber = EditionNumber.From(1), LatestUpdateNumber = UpdateNumber.From(0) },
                new() { ProductName = ProductName.From("101GB004DEVQK"), LatestEditionNumber = EditionNumber.From(2), LatestUpdateNumber = UpdateNumber.From(0) }
            };

            var lastModified = DateTime.UtcNow;
            var salesCatalogueResponse = new ProductList
            {
                ResponseCode = HttpStatusCode.OK,
                Products = products
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductVersionListAsync(job.DataStandardTimestamp, job))
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
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList(),
                RequestedFilter = "",
                DataStandardTimestamp = DateTime.UtcNow.AddDays(-1)
            };

            var build = new S100Build();
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var salesCatalogueResponse = new ProductList
            {
                ResponseCode = HttpStatusCode.OK,
                Products = []
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductVersionListAsync(job.DataStandardTimestamp, job))
                .Returns(Task.FromResult((salesCatalogueResponse, (DateTime?)DateTime.UtcNow)));

            var result = await _getProductsForDataStandardNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndSalesCatalogueReturnsNotModified_ThenReturnsSucceededAndSignalsNoBuildRequired()
        {
            var job = new Job
            {
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList(),
                RequestedFilter = "",
                DataStandardTimestamp = DateTime.UtcNow.AddDays(-1)
            };

            var build = new S100Build();
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var lastModified = DateTime.UtcNow;
            var salesCatalogueResponse = new ProductList
            {
                ResponseCode = HttpStatusCode.NotModified,
                Products = []
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductVersionListAsync(job.DataStandardTimestamp, job))
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
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList(),
                RequestedFilter = "",
                DataStandardTimestamp = DateTime.UtcNow.AddDays(-1)
            };

            var build = new S100Build();
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var salesCatalogueResponse = new ProductList
            {
                ResponseCode = HttpStatusCode.InternalServerError,
                Products = []
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductVersionListAsync(job.DataStandardTimestamp, job))
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
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList(),
                RequestedFilter = "",
                DataStandardTimestamp = DateTime.UtcNow.AddDays(-1)
            };

            var build = new S100Build();
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var salesCatalogueResponse = new ProductList
            {
                ResponseCode = HttpStatusCode.BadRequest,
                Products = []
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductVersionListAsync(job.DataStandardTimestamp, job))
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
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList(),
                RequestedFilter = "",
                DataStandardTimestamp = null
            };

            var build = new S100Build();
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var products = new List<Product>
            {
                new() { ProductName = ProductName.From("101GB004DEVQP"), LatestEditionNumber = EditionNumber.From(1), LatestUpdateNumber = UpdateNumber.From(0) },
                new() { ProductName = ProductName.From("101GB004DEVQK"), LatestEditionNumber = EditionNumber.From(2), LatestUpdateNumber = UpdateNumber.From(0) }
            };

            var salesCatalogueResponse = new ProductList
            {
                ResponseCode = HttpStatusCode.OK,
                Products = products
            };

            A.CallTo(() => _salesCatalogueClient.GetS100ProductVersionListAsync(null, job))
                .Returns(Task.FromResult((salesCatalogueResponse, (DateTime?)DateTime.UtcNow)));

            var result = await _getProductsForDataStandardNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));

            A.CallTo(() => _salesCatalogueClient.GetS100ProductVersionListAsync(null, job))
                .MustHaveHappenedOnceExactly();
        }
    }
}
