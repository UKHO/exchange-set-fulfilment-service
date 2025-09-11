using System.Net;
using FakeItEasy;
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
    internal class S100ProductEditionRetrievalNodeTests
    {
        private IExecutionContext<PipelineContext<S100Build>> _executionContext;
        private AssemblyNodeEnvironment _nodeEnvironment;
        private IProductService _productService;
        private ILogger<S100ProductEditionRetrievalNode> _logger;
        private Job _job;
        private S100Build _build;
        private PipelineContext<S100Build> _pipelineContext;
        private S100ProductEditionRetrievalNode _node;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _nodeEnvironment = A.Fake<AssemblyNodeEnvironment>();
            _productService = A.Fake<IProductService>();
            _logger = A.Fake<ILogger<S100ProductEditionRetrievalNode>>();
            _executionContext = A.Fake<IExecutionContext<PipelineContext<S100Build>>>();
            _node = new S100ProductEditionRetrievalNode(_nodeEnvironment, _productService, _logger);
        }

        [SetUp]
        public void Setup()
        {
            _job = new Job
            {
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList(),
                RequestedFilter = string.Empty,
            };

            _build = new S100Build
            {
                Products = [new() { ProductName = ProductName.From("101GB004DEVQK") }]
            };

            _pipelineContext = new PipelineContext<S100Build>(_job, _build, A.Fake<IStorageService>());
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledAndJobStateIsCreated_ThenReturnsTrue()
        {
            _job.ValidateAndSet(JobState.Created, BuildState.None);

            var result = await _node.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledAndJobStateIsNotCreated_ThenReturnsFalse()
        {
            _job.ValidateAndSet(JobState.UpToDate, BuildState.NotScheduled);

            var result = await _node.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenExecuteAsyncIsCalledWithRequestedProducts_ThenCallsProductServiceWithRequestedProducts()
        {
            _job = new Job
            {
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList
                {
                    ProductName.From("101TE024879"),
                    ProductName.From("101TE024878")
                },
                RequestedFilter = string.Empty
            };

            _pipelineContext = new PipelineContext<S100Build>(_job, _build, A.Fake<IStorageService>());
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);

            var productEditionList = new ProductEditionList { ResponseCode = HttpStatusCode.OK };
            productEditionList.ProductCountSummary.ReturnedProductCount = ProductCount.From(2);

            productEditionList.Add(new ProductEdition
            {
                ProductName = ProductName.From("101TE024879"),
                EditionNumber = EditionNumber.From(1)
            });
            productEditionList.Add(new ProductEdition
            {
                ProductName = ProductName.From("101TE024878"),
                EditionNumber = EditionNumber.From(1)
            });

            A.CallTo(() => _productService.GetProductEditionListAsync(
                    A<DataStandard>.That.IsEqualTo(DataStandard.S100),
                    A<IEnumerable<ProductName>>.That.Matches(p => p.Count() == 2),
                    A<Job>.That.IsEqualTo(_job),
                    A<CancellationToken>.Ignored))
                .Returns(productEditionList);

            _job.ValidateAndSet(JobState.Created, BuildState.None);

            var result = await _node.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            A.CallTo(() => _productService.GetProductEditionListAsync(
                    DataStandard.S100,
                    A<IEnumerable<ProductName>>.That.Matches(p => p.Count() == 2),
                    _job,
                    A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenExecuteAsyncIsCalledWithNoRequestedProductsAndBuildProducts_ThenCallsProductServiceWithBuildProducts()
        {
            _build.Products = [
                new() { ProductName = ProductName.From("BUILD001") },
                new() { ProductName = ProductName.From("BUILD002") }
            ];

            var productEditionList = new ProductEditionList { ResponseCode = HttpStatusCode.OK };
            productEditionList.ProductCountSummary.ReturnedProductCount = ProductCount.From(2);

            A.CallTo(() => _productService.GetProductEditionListAsync(
                A<DataStandard>.That.IsEqualTo(DataStandard.S100),
                A<IEnumerable<ProductName>>.That.Matches(p => p.Count() == 2),
                A<Job>.That.IsEqualTo(_job),
                A<CancellationToken>.Ignored))
                .Returns(productEditionList);

            var result = await _node.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            A.CallTo(() => _productService.GetProductEditionListAsync(
                DataStandard.S100,
                A<IEnumerable<ProductName>>.That.Matches(p => p.Count() == 2),
                _job,
                A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenExecuteAsyncIsCalledAndSomeRequestedProductsAreMissing_ThenLogsWarningAndContinues()
        {
            var productEditionList = new ProductEditionList { ResponseCode = HttpStatusCode.OK };
            productEditionList.ProductCountSummary.ReturnedProductCount = ProductCount.From(1);
            productEditionList.ProductCountSummary.MissingProducts.Add(
                new MissingProduct { ProductName = ProductName.From("101TE024879") }  // Changed from "S-100" to valid format
            );

            A.CallTo(() => _productService.GetProductEditionListAsync(
                    A<DataStandard>.Ignored,
                    A<IEnumerable<ProductName>>.Ignored,
                    A<Job>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(productEditionList);

            var result = await _node.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenExecuteAsyncIsCalledAndServiceReturnsNonOKStatus_ThenSignalsAssemblyErrorAndReturnsFailed()
        {
            var productEditionList = new ProductEditionList { ResponseCode = HttpStatusCode.BadRequest };

            A.CallTo(() => _productService.GetProductEditionListAsync(
                A<DataStandard>.Ignored,
                A<IEnumerable<ProductName>>.Ignored,
                A<Job>.Ignored,
                A<CancellationToken>.Ignored))
                .Returns(productEditionList);

            var result = await _node.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenExecuteAsyncIsCalledAndServiceReturnsSuccessfully_ThenUpdatesProductEditionsAndSignalsBuildRequired()
        {
            var productEditionList = new ProductEditionList { ResponseCode = HttpStatusCode.OK };
            productEditionList.ProductCountSummary.ReturnedProductCount = ProductCount.From(1);
            var productEditions = new List<ProductEdition>
            {
                new()
                {
                    ProductName = ProductName.From("101TE024879"),
                    EditionNumber = EditionNumber.From(1)
                }
            };
            foreach (var productEdition in productEditions)
            {
                productEditionList.Add(productEdition);
            }

            A.CallTo(() => _productService.GetProductEditionListAsync(
                A<DataStandard>.Ignored,
                A<IEnumerable<ProductName>>.Ignored,
                A<Job>.Ignored,
                A<CancellationToken>.Ignored))
                .Returns(productEditionList);

            var result = await _node.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(_build.ProductEditions, Is.EqualTo(productEditions));
        }
    }
}
