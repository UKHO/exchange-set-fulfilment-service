using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.ExternalErrors;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Pipelines.Assembly.Nodes.S100
{
    [TestFixture]
    internal class GetS100ProductUpdatesSinceNodeTests
    {
        private IExecutionContext<PipelineContext<S100Build>> _executionContext;
        private AssemblyNodeEnvironment _nodeEnvironment;
        private IProductService _productService;
        private ILogger<GetS100ProductUpdatesSinceNode> _logger;
        private IConfiguration _configuration;
        private Job? _job;
        private S100Build? _build;
        private PipelineContext<S100Build>? _pipelineContext;
        private GetS100ProductUpdatesSinceNode? _node;
        private const string RequestedFilter = "2025-08-30T07:28:00.000Z";
        private const string ProductIdentifier = "101ABCDEF";

        [SetUp]
        public void SetUp()
        {
            _executionContext = A.Fake<IExecutionContext<PipelineContext<S100Build>>>();
            _productService = A.Fake<IProductService>();
            _logger = A.Fake<ILogger<GetS100ProductUpdatesSinceNode>>();

            _nodeEnvironment = new AssemblyNodeEnvironment(_configuration, CancellationToken.None, _logger);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledAndJobStateIsCreatedAndExchangeSetTypeIsUpdatesSince_ThenReturnsTrue()
        {
            SetupJobAndBuild();
            _node = new GetS100ProductUpdatesSinceNode(_nodeEnvironment, _productService);

            var result = await _node.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledAndJobStateIsNotCreated_ThenReturnsFalse()
        {
            SetupJobAndBuild(JobState.Completed);
            _node = new GetS100ProductUpdatesSinceNode(_nodeEnvironment, _productService);

            var result = await _node.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndServiceReturnsProducts_ThenReturnsSucceeded()
        {
            SetupJobAndBuild();
            var productEditionList = new ProductEditionList
            {
                ProductCountSummary = new ProductCountSummary
                {
                    RequestedProductsAlreadyUpToDateCount = ProductCount.From(2),
                    MissingProducts = new MissingProductList()
                }
            };
            productEditionList.Add(new ProductEdition { ProductName = ProductName.From(ProductIdentifier) });

            A.CallTo(() => _productService.GetS100ProductUpdatesSinceAsync(_job!.RequestedFilter, _job.ProductIdentifier, _job, A<CancellationToken>.Ignored))
                .Returns((productEditionList,null));

            _node = new GetS100ProductUpdatesSinceNode(_nodeEnvironment, _productService);

            var result = await _node.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndServiceReturnsNull_ThenReturnsFailed()
        {
            SetupJobAndBuild();
            A.CallTo(() => _productService.GetS100ProductUpdatesSinceAsync(_job!.RequestedFilter, _job.ProductIdentifier, _job, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult<(ProductEditionList, ExternalServiceError?)>((null!, null)));

            _node = new GetS100ProductUpdatesSinceNode(_nodeEnvironment, _productService);

            var result = await _node.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndServiceThrowsException_ThenReturnsFailed()
        {
            SetupJobAndBuild();
            A.CallTo(() => _productService.GetS100ProductUpdatesSinceAsync(_job!.RequestedFilter, _job.ProductIdentifier, _job, A<CancellationToken>.Ignored))
                .Throws(new Exception("Simulated service failure"));

            _node = new GetS100ProductUpdatesSinceNode(_nodeEnvironment, _productService);

            var result = await _node.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledAndExchangeSetTypeIsNotUpdatesSince_ThenReturnsFalse()
        {
            SetupJobAndBuild(JobState.Created, ExchangeSetType.ProductNames);
            _node = new GetS100ProductUpdatesSinceNode(_nodeEnvironment, _productService);

            var result = await _node.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndProductEditionListIsEmpty_ThenReturnsFailed()
        {
            SetupJobAndBuild();
            var productEditionList = new ProductEditionList
            {
                ProductCountSummary = new ProductCountSummary()
            };

            A.CallTo(() => _productService.GetS100ProductUpdatesSinceAsync(_job!.RequestedFilter, _job.ProductIdentifier, _job, A<CancellationToken>.Ignored))
                .Returns((productEditionList, null));

            _node = new GetS100ProductUpdatesSinceNode(_nodeEnvironment, _productService);

            var result = await _node.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndServiceReturnsNotModified_ThenNodeSucceeded()
        {
            SetupJobAndBuild();
            var productEditionList = new ProductEditionList
            {
                ProductsLastModified = DateTime.UtcNow.AddDays(-1),
            };

            var externalServiceError = new ExternalServiceError(System.Net.HttpStatusCode.NotModified, ExternalServiceName.SalesCatalogueService);

            A.CallTo(() => _productService.GetS100ProductUpdatesSinceAsync(_job!.RequestedFilter, _job.ProductIdentifier, _job, A<CancellationToken>.Ignored))
                .Returns((productEditionList, externalServiceError));

            _node = new GetS100ProductUpdatesSinceNode(_nodeEnvironment, _productService);

            var result = await _node.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndServiceReturnsNotOk_ThenNodeFailed()
        {
            SetupJobAndBuild();
            var productEditionList = new ProductEditionList();

            var externalServiceError = new ExternalServiceError(System.Net.HttpStatusCode.InternalServerError, ExternalServiceName.SalesCatalogueService);

            A.CallTo(() => _productService.GetS100ProductUpdatesSinceAsync(_job!.RequestedFilter, _job.ProductIdentifier, _job, A<CancellationToken>.Ignored))
                .Returns((productEditionList, null));

            _node = new GetS100ProductUpdatesSinceNode(_nodeEnvironment, _productService);

            var result = await _node.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        private void SetupJobAndBuild(JobState jobState = JobState.Created, ExchangeSetType exchangeSetType = ExchangeSetType.UpdatesSince)
        {
            var job = new UKHO.ADDS.EFS.Domain.Jobs.Job
            {
                Id = JobId.From("job-1"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedFilter = RequestedFilter, // Provide a default filter value for tests
                ProductIdentifier = DataStandardProduct.From((int)DataStandardProductType.S101),
                RequestedProducts = new ProductNameList(),
                ExchangeSetType = exchangeSetType
            };
            job.ValidateAndSet(jobState, BuildState.None);

            _job = job;

            _build = new S100Build
            {
                Products = new List<Product> { new Product { ProductName = ProductName.From(ProductIdentifier) } }
            };
            _pipelineContext = new PipelineContext<S100Build>(_job, _build, A.Fake<IStorageService>());
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);
        }
    }
}
