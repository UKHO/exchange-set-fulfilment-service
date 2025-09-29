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
    internal class GetS100ProductNamesNodeTests
    {
        private IExecutionContext<PipelineContext<S100Build>> _executionContext;
        private AssemblyNodeEnvironment _nodeEnvironment;
        private IProductService _productService;
        private ILogger<GetS100ProductNamesNode> _logger;
        private IStorageService _storageService;
        private IConfiguration _configuration;
        private GetS100ProductNamesNode _getS100ProductNamesNode;
        private CancellationToken _cancellationToken;

        private const string DefaultMaxExchangeSetSizeMB = "100";
        private const string DefaultExchangeSetExpiresIn = "01:00:00";
        private const string TestJobId = "test-job-id";
        private const string TestCallbackUri = "https://test.com/callback";
        private const string TestProductName1 = "101GB004DEVQK";
        private const string TestProductName2 = "102CA005N5040W00130.h5";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _executionContext = A.Fake<IExecutionContext<PipelineContext<S100Build>>>();
            _productService = A.Fake<IProductService>();
            _logger = A.Fake<ILogger<GetS100ProductNamesNode>>();
            _storageService = A.Fake<IStorageService>();
            _cancellationToken = CancellationToken.None;

            _configuration = CreateTestConfiguration();
            _nodeEnvironment = new AssemblyNodeEnvironment(_configuration, _cancellationToken, A.Fake<ILogger>());
        }

        [SetUp]
        public void Setup()
        {
            _getS100ProductNamesNode = new GetS100ProductNamesNode(_nodeEnvironment, _productService, _logger);
        }

        [Test]
        public void WhenProductServiceIsNull_ThenConstructorThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new GetS100ProductNamesNode(_nodeEnvironment, null!, _logger));

            Assert.That(exception.ParamName, Is.EqualTo("productService"));
        }

        [Test]
        public void WhenLoggerIsNull_ThenConstructorThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new GetS100ProductNamesNode(_nodeEnvironment, _productService, null!));

            Assert.That(exception.ParamName, Is.EqualTo("logger"));
        }

        [TestCase(RequestType.ProductNames, JobState.Created, ExpectedResult = true)]
        [TestCase(RequestType.Internal, JobState.Created, ExpectedResult = true)]
        [TestCase(RequestType.ProductVersions, JobState.Created, ExpectedResult = false)]
        [TestCase(RequestType.UpdatesSince, JobState.Created, ExpectedResult = false)]
        [TestCase(RequestType.ProductNames, JobState.UpToDate, ExpectedResult = false)]
        public async Task<bool> WhenJobStateAndRequestTypeProvided_ThenShouldExecuteAsyncReturnsCorrectResult(
            RequestType requestType, JobState jobState)
        {
            var job = CreateTestJob(requestType: requestType);
            job.ValidateAndSet(jobState, BuildState.NotScheduled);
            var pipelineContext = CreatePipelineContext(job);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);

            return await _getS100ProductNamesNode.ShouldExecuteAsync(_executionContext);
        }

        [Test]
        public async Task WhenProductServiceReturnsOkWithValidProducts_ThenExecuteAsyncReturnsSucceeded()
        {
            var requestedProducts = CreateProductNameList(TestProductName1, TestProductName2);
            var job = CreateTestJob(requestedProducts: requestedProducts);
            var productEditionList = CreateSuccessfulProductEditionList();

            SetupExecutionContext(job);
            SetupProductService(productEditionList);

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(_executionContext.Subject.Build.ProductEditions, Is.EqualTo(productEditionList.Products));
        }

        [Test]
        public async Task WhenJobHasNoRequestedProducts_ThenExecuteAsyncUsesProductsFromBuild()
        {
            var job = CreateTestJob(requestedProducts: new ProductNameList());
            var build = CreateS100BuildWithProducts();
            var productEditionList = CreateSuccessfulProductEditionList();

            SetupExecutionContextWithBuild(job, build);
            SetupProductService(productEditionList);

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            A.CallTo(() => _productService.GetProductEditionListAsync(
                    DataStandard.S100,
                    A<IEnumerable<ProductName>>.That.Contains(ProductName.From(TestProductName1)),
                    job,
                    CancellationToken.None))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenBuildHasNullProducts_ThenExecuteAsyncUsesEmptyProductList()
        {
            var job = CreateTestJob(requestedProducts: new ProductNameList());
            var build = new S100Build { Products = null };
            var productEditionList = CreateSuccessfulProductEditionList();

            SetupExecutionContextWithBuild(job, build);
            SetupProductService(productEditionList);

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            A.CallTo(() => _productService.GetProductEditionListAsync(
                    DataStandard.S100,
                    A<IEnumerable<ProductName>>.That.IsEmpty(),
                    job,
                    CancellationToken.None))
                .MustHaveHappenedOnceExactly();
        }

        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.InternalServerError)]
        [TestCase(HttpStatusCode.NotFound)]
        public async Task WhenProductServiceReturnsNonOkResponse_ThenExecuteAsyncReturnsFailed(HttpStatusCode statusCode)
        {
            var requestedProducts = CreateProductNameList(TestProductName1);
            var job = CreateTestJob(requestedProducts: requestedProducts);
            var productEditionList = new ProductEditionList { ResponseCode = statusCode };

            SetupExecutionContext(job);
            SetupProductService(productEditionList);

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenTotalFileSizeExceedsLimit_ThenExecuteAsyncReturnsFailedAndSetsErrorResponse()
        {
            var requestedProducts = CreateProductNameList(TestProductName1, TestProductName2);
            var job = CreateTestJob(requestedProducts: requestedProducts);
            var productEditionList = CreateProductEditionListExceedingSize();

            SetupExecutionContext(job);
            SetupProductService(productEditionList);

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
            Assert.That(_executionContext.Subject.ErrorResponse, Is.Not.Null);
            Assert.That(_executionContext.Subject.ErrorResponse.Errors.First().Source, Is.EqualTo("exchangeSetSize"));
        }

        [Test]
        public async Task WhenTotalFileSizeEqualsLimit_ThenExecuteAsyncReturnsSucceeded()
        {
            var requestedProducts = CreateProductNameList(TestProductName1);
            var job = CreateTestJob(requestedProducts: requestedProducts);
            var productEditionList = CreateProductEditionListAtSizeLimit();

            SetupExecutionContext(job);
            SetupProductService(productEditionList);

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenRequestTypeIsProductNames_ThenExecuteAsyncSetsJobProperties()
        {
            var requestedProducts = CreateProductNameList(TestProductName1);
            var job = CreateTestJob(requestedProducts: requestedProducts, requestType: RequestType.ProductNames);
            var productEditionList = CreateSuccessfulProductEditionList();

            SetupExecutionContext(job);
            SetupProductService(productEditionList);

            await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(job.ExchangeSetUrlExpiryDateTime, Is.Not.Null);
            Assert.That(job.RequestedProductCount, Is.EqualTo(ProductCount.From(1)));
            Assert.That(job.ExchangeSetProductCount, Is.EqualTo(productEditionList.Count));
        }

        [Test]
        public async Task WhenRequestTypeIsNotProductNames_ThenExecuteAsyncDoesNotSetJobProperties()
        {
            var requestedProducts = CreateProductNameList(TestProductName1);
            var job = CreateTestJob(requestedProducts: requestedProducts, requestType: RequestType.Internal);
            var productEditionList = CreateSuccessfulProductEditionList();
            var originalExpiryDateTime = job.ExchangeSetUrlExpiryDateTime;

            SetupExecutionContext(job);
            SetupProductService(productEditionList);

            await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(job.ExchangeSetUrlExpiryDateTime, Is.EqualTo(originalExpiryDateTime));
        }

        [Test]
        public async Task WhenProductEditionListIsEmpty_ThenExecuteAsyncReturnsSucceeded()
        {
            var requestedProducts = CreateProductNameList(TestProductName1);
            var job = CreateTestJob(requestedProducts: requestedProducts);
            var productEditionList = new ProductEditionList { ResponseCode = HttpStatusCode.OK };

            SetupExecutionContext(job);
            SetupProductService(productEditionList);

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
            Assert.That(_executionContext.Subject.Build.ProductEditions, Is.Empty);
        }


        [Test]
        public async Task WhenProductEditionListHasMissingProducts_ThenLogSalesCatalogueProductsNotReturnedIsCalled()
        {
            var requestedProducts = CreateProductNameList(TestProductName1, TestProductName2);
            var job = CreateTestJob(requestedProducts: requestedProducts);
            var productEditionList = CreateProductEditionListWithMissingProducts();

            SetupExecutionContext(job);
            SetupProductService(productEditionList);

            var result = await _getS100ProductNamesNode.ExecuteAsync(_executionContext);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        private static ProductEditionList CreateProductEditionListWithMissingProducts()
        {
            var missingProducts = new MissingProductList();
            missingProducts.Add(new MissingProduct { ProductName = ProductName.From("101GB00ABCD") });

            var productCountSummary = new ProductCountSummary
            {
                MissingProducts = missingProducts
            };

            var list = new ProductEditionList
            {
                ResponseCode = HttpStatusCode.OK,
                ProductCountSummary = productCountSummary
            };

            list.Add(new ProductEdition { ProductName = ProductName.From(TestProductName1), FileSize = 1000 });
            return list;
        }

        private static IConfiguration CreateTestConfiguration(
            string maxSizeMB = DefaultMaxExchangeSetSizeMB,
            string expiresIn = DefaultExchangeSetExpiresIn)
        {
            var configurationData = new Dictionary<string, string>
                {
                    { "orchestrator:Response:MaxExchangeSetSizeInMB", maxSizeMB },
                    { "orchestrator:Response:ExchangeSetExpiresIn", expiresIn }
                };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();
        }

        private static Job CreateTestJob(
            ProductNameList? requestedProducts = null,
            RequestType requestType = RequestType.ProductNames)
        {
            return new Job
            {
                Id = JobId.From(TestJobId),
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = requestedProducts ?? new ProductNameList(),
                RequestedFilter = string.Empty,
                RequestType = requestType,
                CallbackUri = CallbackUri.From(new Uri(TestCallbackUri)),
                ProductIdentifier = DataStandardProduct.Undefined
            };
        }

        private static ProductNameList CreateProductNameList(params string[] productNames)
        {
            var list = new ProductNameList();
            foreach (var name in productNames)
            {
                list.Add(ProductName.From(name));
            }

            return list;
        }

        private static S100Build CreateS100BuildWithProducts()
        {
            return new S100Build
            {
                Products = new List<Product>
                    {
                        new() { ProductName = ProductName.From(TestProductName1) },
                        new() { ProductName = ProductName.From(TestProductName2) }
                    }
            };
        }

        private static ProductEditionList CreateSuccessfulProductEditionList()
        {
            var list = new ProductEditionList { ResponseCode = HttpStatusCode.OK };
            list.Add(new ProductEdition { ProductName = ProductName.From(TestProductName1), FileSize = 1000 });
            return list;
        }

        private static ProductEditionList CreateProductEditionListExceedingSize()
        {
            var list = new ProductEditionList { ResponseCode = HttpStatusCode.OK };
            list.Add(new ProductEdition
            {
                ProductName = ProductName.From(TestProductName1),
                FileSize = 600 * 1024 * 1024
            });
            list.Add(new ProductEdition
            {
                ProductName = ProductName.From(TestProductName2),
                FileSize = 500 * 1024 * 1024
            });
            return list;
        }

        private static ProductEditionList CreateProductEditionListAtSizeLimit()
        {
            var list = new ProductEditionList { ResponseCode = HttpStatusCode.OK };
            list.Add(new ProductEdition
            {
                ProductName = ProductName.From(TestProductName1),
                FileSize = 100 * 1024 * 1024
            });
            return list;
        }

        private void SetupExecutionContext(Job job)
        {
            var pipelineContext = CreatePipelineContext(job);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);
        }

        private void SetupExecutionContextWithBuild(Job job, S100Build build)
        {
            var pipelineContext = new PipelineContext<S100Build>(job, build, _storageService);
            A.CallTo(() => _executionContext.Subject).Returns(pipelineContext);
        }

        private PipelineContext<S100Build> CreatePipelineContext(Job job)
        {
            var build = new S100Build();
            return new PipelineContext<S100Build>(job, build, _storageService);
        }

        private void SetupProductService(ProductEditionList productEditionList)
        {
            A.CallTo(() => _productService.GetProductEditionListAsync(
                    A<DataStandard>._,
                    A<IEnumerable<ProductName>>._,
                    A<Job>._,
                    A<CancellationToken>._))
                .Returns(productEditionList);
        }
    }
}
