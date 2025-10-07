using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
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
    internal class GetS100ProductVersionsNodeTest
    {
        private IProductService _productService;
        private ILogger<GetS100ProductVersionsNode> _logger;
        private AssemblyNodeEnvironment _assemblyNodeEnvironment;
        private GetS100ProductVersionsNode _s100ProductVersionsNode;
        private PipelineContext<S100Build>? _pipelineContext;
        private IExecutionContext<PipelineContext<S100Build>> _executionContext;
        private S100Build _s100Build;
        private IConfiguration _configuration;
        private IStorageService _storageService;
        private const string ValidProductName = "101GB40079ABCDEFG";

        [SetUp]
        public void SetUp()
        {
            _productService = A.Fake<IProductService>();
            _logger = A.Fake<ILogger<GetS100ProductVersionsNode>>();
            _configuration = A.Fake<IConfiguration>();
            _storageService = A.Fake<IStorageService>();
            _assemblyNodeEnvironment = new AssemblyNodeEnvironment(_configuration, default, _logger);
            _s100ProductVersionsNode = new GetS100ProductVersionsNode(_assemblyNodeEnvironment, _productService, _logger);
            _s100Build = new S100Build();
            _executionContext = A.Fake<IExecutionContext<PipelineContext<S100Build>>>();

            var fakeConfiguration = A.Fake<IConfigurationSection>();
        }

        [TestCase(JobState.Created, ExchangeSetType.ProductVersions, true)]
        [TestCase(JobState.Completed, ExchangeSetType.ProductVersions, false)]
        [TestCase(JobState.Created, ExchangeSetType.ProductNames, false)]
        [TestCase(JobState.Completed, ExchangeSetType.ProductNames, false)]
        public async Task WhenShouldExecuteAsyncIsCalled_ThenReturnsExpected(JobState jobState, ExchangeSetType exchangeSetType, bool expected)
        {
            var job = CreateJob(exchangeSetType, null, jobState);
            _pipelineContext = new PipelineContext<S100Build>(job, _s100Build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);
            var result = await _s100ProductVersionsNode.ShouldExecuteAsync(_executionContext);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void WhenCallWithNullProductService_ThenThrowsArgumentNullException()
        {
            Assert.That(() => new GetS100ProductVersionsNode(_assemblyNodeEnvironment, null!, _logger), Throws.ArgumentNullException);
        }

        [Test]
        public void WhenCallWithNullLogger_ThenThrowsArgumentNullException()
        {
            Assert.That(() => new GetS100ProductVersionsNode(_assemblyNodeEnvironment, _productService, null!), Throws.ArgumentNullException);
        }

        [Test]
        public async Task WhenExecuteAsyncReturnsOKWithMissingProducts_ThenSetsBuildAndJobProperties()
        {
            var productVersions = new ProductVersionList { new() { ProductName = ProductName.From(ValidProductName), EditionNumber = EditionNumber.From(1) } };
            var job = CreateJob(ExchangeSetType.ProductVersions, productVersions);

            _pipelineContext = new PipelineContext<S100Build>(job, _s100Build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);
            var editionList = CreateProductEditionList(HttpStatusCode.OK, true);
          
            A.CallTo(() => _productService.GetProductVersionsListAsync(A<DataStandard>.Ignored, A<ProductVersionList>.Ignored, A<Job>.Ignored, A<CancellationToken>.Ignored)).Returns(Task.FromResult(editionList));

            var result = await _s100ProductVersionsNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(_s100Build.ProductEditions, Is.EqualTo(editionList));
            Assert.That(job.RequestedProductCount, Is.EqualTo(ProductCount.From(productVersions.Count())));
            Assert.That(job.ExchangeSetProductCount, Is.EqualTo(editionList.Count));
            Assert.That(job.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(editionList.ProductCountSummary.RequestedProductsAlreadyUpToDateCount));
            Assert.That(job.RequestedProductsNotInExchangeSet, Is.EqualTo(editionList.ProductCountSummary.MissingProducts));
        }

        [Test]
        public async Task WhenExecuteAsyncReturnsBadRequest_ThenNodeFailed()
        {
            var productVersions = new ProductVersionList { new() { ProductName = ProductName.From(ValidProductName), EditionNumber = EditionNumber.From(1) } };
            var job = CreateJob(ExchangeSetType.ProductVersions, productVersions);
            _pipelineContext = new PipelineContext<S100Build>(job, _s100Build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);

            var editionList = CreateProductEditionList(HttpStatusCode.BadRequest);
            A.CallTo(() => _productService.GetProductVersionsListAsync(DataStandard.S100, productVersions, job, default)).Returns(Task.FromResult(editionList));

            var result = await _s100ProductVersionsNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenExecuteAsyncWithNullProductVersions_ThenNodeFailed()
        {
            var job = CreateJob(ExchangeSetType.ProductVersions, null);
            _pipelineContext = new PipelineContext<S100Build>(job, _s100Build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);

            var editionList = CreateProductEditionList(HttpStatusCode.OK);
            A.CallTo(() => _productService.GetProductVersionsListAsync(DataStandard.S100, null, job, default)).Returns(Task.FromResult(editionList));

            var result = await _s100ProductVersionsNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenExecuteAsyncWithEmptyProductVersions_ThenNodeSucceeded()
        {
            var job = CreateJob(ExchangeSetType.ProductVersions, []);
            _pipelineContext = new PipelineContext<S100Build>(job, _s100Build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);

            var editionList = CreateProductEditionList(HttpStatusCode.OK);
            A.CallTo(() => _productService.GetProductVersionsListAsync(DataStandard.S100, job.ProductVersions, job, default)).Returns(Task.FromResult(editionList));

            var result = await _s100ProductVersionsNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }


        private static Job CreateJob(ExchangeSetType exchangeSetType, ProductVersionList productVersions, JobState jobState = JobState.Created)
        {
            var job = new Job
            {
                Id = JobId.From("job-1"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = [],
                RequestedFilter = "",
                ExchangeSetType = exchangeSetType,
                ProductVersions = productVersions
            };
            if (jobState != JobState.Created)
                job.ValidateAndSet(jobState, BuildState.None);
            return job;
        }

        private static ProductEditionList CreateProductEditionList(HttpStatusCode code, bool hasMissingProducts = false, int productCount = 1)
        {
            var missingList = new MissingProductList();
            if (hasMissingProducts)
                missingList.Add(new MissingProduct { });
            var editionList = new ProductEditionList
            {
                ResponseCode = code,
                ProductCountSummary = new ProductCountSummary
                {
                    MissingProducts = missingList,
                    RequestedProductsAlreadyUpToDateCount = ProductCount.From(2)
                },
            };
            for (var i = 0; i < productCount; i++)
                editionList.Add(new ProductEdition());
            return editionList;
        }
    }
}
