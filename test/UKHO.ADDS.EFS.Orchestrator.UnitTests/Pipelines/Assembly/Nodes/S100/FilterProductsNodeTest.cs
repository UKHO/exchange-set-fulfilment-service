using FakeItEasy;
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
    internal class FilterProductsNodeTest
    {
        private IExecutionContext<PipelineContext<S100Build>> _executionContext;
        private AssemblyNodeEnvironment _nodeEnvironment;
        private Job _job;
        private S100Build _build;
        private PipelineContext<S100Build> _pipelineContext;
        private IStorageService _storageService;

        private FilterProductsNode _filterProductsNode;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _nodeEnvironment = A.Fake<AssemblyNodeEnvironment>();
            _executionContext = A.Fake<IExecutionContext<PipelineContext<S100Build>>>();
            _storageService = A.Fake<IStorageService>();
            _filterProductsNode = new FilterProductsNode(_nodeEnvironment);
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
                RequestedFilter = "productName eq '101GB004DEVQK'",
            };

            _build = new S100Build
            {
                Products =
                [
                    new() { ProductName = ProductName.From("101GB004DEVQK"), LatestEditionNumber = EditionNumber.From(1) },
                    new() {ProductName = ProductName.From("101GB00510210"), LatestEditionNumber = EditionNumber.From(2)},
                    new() {ProductName = ProductName.From("102CA005N5040W00130.h5"), LatestEditionNumber = EditionNumber.From(1)}
                ]
            };

            _pipelineContext = new PipelineContext<S100Build>(_job, _build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledAndJobStateIsCreatedAndFilterExistsAndProductsExist_ThenReturnsTrue()
        {
            var result = await _filterProductsNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledAndJobStateIsNotCreated_ThenReturnsFalse()
        {
            _job.ValidateAndSet(JobState.UpToDate, BuildState.NotScheduled);

            var result = await _filterProductsNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledAndRequestedFilterIsEmpty_ThenReturnsFalse()
        {
            var job = new Job
            {
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList(),
                RequestedFilter = string.Empty
            };

            var build = new S100Build
            {
                Products =
                [
                    new() { ProductName = ProductName.From("101GB004DEVQK") }
                ]
            };

            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var result = await _filterProductsNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledAndRequestedFilterIsNull_ThenReturnsFalse()
        {
            var job = new Job
            {
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList(),
                RequestedFilter = null!
            };

            var build = new S100Build
            {
                Products =
                [
                    new() { ProductName = ProductName.From("101GB004DEVQK") }
                ]
            };

            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var result = await _filterProductsNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledAndProductsIsEmpty_ThenReturnsFalse()
        {
            var job = new Job
            {
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList(),
                RequestedFilter = "productName eq '101GB004DEVQK'",
            };

            var build = new S100Build
            {
                Products = []
            };

            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var result = await _filterProductsNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenShouldExecuteAsyncIsCalledAndProductsIsNull_ThenReturnsFalse()
        {
            var job = new Job
            {
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList(),
                RequestedFilter = "productName eq '101GB004DEVQK'",
            };

            var build = new S100Build
            {
                Products = null
            };

            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            var result = await _filterProductsNode.ShouldExecuteAsync(_executionContext);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenPerformExecuteAsyncIsCalledAndFilterMatchesProducts_ThenFiltersProductsAndReturnsSucceeded()
        {
            _job = new Job
            {
                Id = JobId.From("test-job-id"),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = new ProductNameList(),
                RequestedFilter = "startswith(ProductName, '102')",
            };

            _build = new S100Build
            {
                Products =
                [
                    new() { ProductName = ProductName.From("101GB004DEVQK"), LatestEditionNumber = EditionNumber.From(1) },
                    new() {ProductName = ProductName.From("101GB00510210"), LatestEditionNumber = EditionNumber.From(2)},
                    new() {ProductName = ProductName.From("102CA005N5040W00130.h5"), LatestEditionNumber = EditionNumber.From(1)},
                    new() {ProductName = ProductName.From("102CA005N5040W00140.h5"), LatestEditionNumber = EditionNumber.From(1)}
                ]
            };

            _pipelineContext = new PipelineContext<S100Build>(_job, _build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);

            var originalProductCount = _build.Products!.Count();

            var result = await _filterProductsNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(_build.Products.Count(), Is.LessThan(originalProductCount));
            Assert.That(_build.Products.Count(), Is.EqualTo(2));
        }

        //[Test]
        //public async Task WhenPerformExecuteAsyncIsCalledAndFilterMatchesNoProducts_ThenSignalsNoBuildRequiredAndReturnsSucceeded()
        //{
        //    var products = new ProductNameList();
            
        //    products.Add(ProductName.From("101product1"));
        //    products.Add(ProductName.From("101product2"));

        //    _job = new Job
        //    {
        //        Id = JobId.From("test-job-id"),
        //        Timestamp = DateTime.UtcNow,
        //        DataStandard = DataStandard.S100,
        //        RequestedProducts = products,
        //        RequestedFilter = "productName eq '101GB004DEVQP'",
        //    };

        //    _build = new S100Build
        //    {
        //        Products =
        //        [
        //            new() { ProductName = ProductName.From("101GB004DEVQK"), LatestEditionNumber = EditionNumber.From(1) },
        //            new() {ProductName = ProductName.From("101GB00510210"), LatestEditionNumber = EditionNumber.From(2)},
        //            new() {ProductName = ProductName.From("102CA005N5040W00130.h5"), LatestEditionNumber = EditionNumber.From(1)}
        //        ]
        //    };

        //    _pipelineContext = new PipelineContext<S100Build>(_job, _build, _storageService);
        //    A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);

        //    var originalProductCount = _build.Products!.Count();

        //    var result = await _filterProductsNode.ExecuteAsync(_executionContext);

        //    Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        //    Assert.That(_build.Products.Count(), Is.EqualTo(originalProductCount));
        //    Assert.That(_job.JobState, Is.EqualTo(JobState.UpToDate));
        //    A.CallTo(() => _storageService.UpdateJobAsync(_job)).MustHaveHappenedOnceExactly();
        //}

        //[Test]
        //public async Task WhenPerformExecuteAsyncIsCalledAndComplexFilter_ThenFiltersCorrectlyAndReturnsSucceeded()
        //{
        //    var products = new ProductNameList();

        //    products.Add(ProductName.From("101product1"));
        //    products.Add(ProductName.From("101product2"));

        //    _job = new Job
        //    {
        //        Id = JobId.From("test-job-id"),
        //        Timestamp = DateTime.UtcNow,
        //        DataStandard = DataStandard.S100,
        //        RequestedProducts = products,
        //        RequestedFilter = "productName eq '101GB004DEVQK' or latestEditionNumber eq 2",
        //    };

        //    _build = new S100Build
        //    {
        //        Products =
        //        [
        //            new() { ProductName = ProductName.From("101GB004DEVQK"), LatestEditionNumber = EditionNumber.From(1) },
        //            new() {ProductName = ProductName.From("101GB00510210"), LatestEditionNumber = EditionNumber.From(2)},
        //            new() {ProductName = ProductName.From("102CA005N5040W00130.h5"), LatestEditionNumber = EditionNumber.From(1)}
        //        ]
        //    };
        //    _pipelineContext = new PipelineContext<S100Build>(_job, _build, _storageService);
        //    A.CallTo(() => _executionContext.Subject).Returns(_pipelineContext);

        //    var result = await _filterProductsNode.ExecuteAsync(_executionContext);

        //    Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        //    Assert.That(_build.Products.Count(), Is.EqualTo(2));
        //    Assert.That(_build.Products.Any(p => p.ProductName == "101GB004DEVQK"), Is.True);
        //    Assert.That(_build.Products.Any(p => p.ProductName == "101GB00510210"), Is.True);
        //}
    }
}
