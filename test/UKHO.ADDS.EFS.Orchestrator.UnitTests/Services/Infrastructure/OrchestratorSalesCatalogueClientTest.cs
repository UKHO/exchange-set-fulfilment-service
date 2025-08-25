using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;


namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Services.Infrastructure
{
    [TestFixture]
    public class OrchestratorSalesCatalogueClientTest
    {
        private ISalesCatalogueKiotaClientAdapter _adapter;
        private ILogger<OrchestratorSalesCatalogueClient> _logger;
        private OrchestratorSalesCatalogueClient _client;
        private Job _job;

        [SetUp]
        public void Setup()
        {
            _adapter = A.Fake<ISalesCatalogueKiotaClientAdapter>();
            _logger = A.Fake<ILogger<OrchestratorSalesCatalogueClient>>();
            _client = new OrchestratorSalesCatalogueClient(_adapter, _logger);
            _job = new Job
            {
                Id = "job",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = "test_product",
                RequestedFilter = "test_filter"
            };
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            var nullAdapterException =
                Assert.Throws<ArgumentNullException>(() => new OrchestratorSalesCatalogueClient(null, _logger));
            var nullLoggerException =
                Assert.Throws<ArgumentNullException>(() => new OrchestratorSalesCatalogueClient(_adapter, null));

            Assert.That(nullAdapterException.ParamName, Is.EqualTo("salesCatalogueKiotaClientAdapter"));
            Assert.That(nullLoggerException.ParamName, Is.EqualTo("logger"));
        }

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncReceivesSuccessWithValidCatalogue_ThenReturnsValidResponseWithLastModifiedDate()
        {
            var catalogueList = new List<S100BasicCatalogue>
            {
                new() { ProductName = "ProductA", LatestEditionNumber = 1, LatestUpdateNumber = 2 }
            };
            var expectedLastModifiedUtc = DateTime.UtcNow;
            var expectedLastModifiedHeader = expectedLastModifiedUtc.ToString("R");

            A.CallTo(() => _adapter.GetBasicCatalogueAsync(
            A<DateTime?>._,
            A<Job>._,
            A<HeadersInspectionHandlerOption>._,
            A<CancellationToken>._))
            .Invokes((DateTime? sinceDateTime, Job job, HeadersInspectionHandlerOption headersOption, CancellationToken cancellationToken) =>
            {
                headersOption.ResponseHeaders["Last-Modified"] = [expectedLastModifiedHeader];
            })
            .Returns(Task.FromResult<List<S100BasicCatalogue>?>(catalogueList));

            var (result, lastModified) = await _client.GetS100ProductsFromSpecificDateAsync(DateTime.UtcNow, _job);

            Assert.Multiple(() =>
            {
                Assert.That(result.ResponseBody.Count, Is.EqualTo(1));
                Assert.That(result.ResponseBody[0].ProductName, Is.EqualTo("ProductA"));
                Assert.That(result.ResponseCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(lastModified?.ToUniversalTime(), Is.EqualTo(TruncateToSeconds(expectedLastModifiedUtc)));
            });
        }

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncReceivesNotModified_ThenReturnsNotModifiedResponseWithLastModifiedDate()
        {
            var expectedLastModifiedUtc = DateTime.UtcNow;
            var expectedLastModifiedHeader = expectedLastModifiedUtc.ToString("R");
            var apiException = new ApiException("Not Modified") { ResponseStatusCode = 304 };
            A.CallTo(() => _adapter.GetBasicCatalogueAsync(
            A<DateTime?>._,
            A<Job>._,
            A<HeadersInspectionHandlerOption>._,
            A<CancellationToken>._))
            .Invokes((DateTime? sinceDateTime, Job job, HeadersInspectionHandlerOption headersOption, CancellationToken cancellationToken) =>
            {
                headersOption.ResponseHeaders["Last-Modified"] = new List<string> { expectedLastModifiedHeader };
            })
            .Throws(apiException);

            var sinceDate = DateTime.UtcNow;
            var (result, lastModified) = await _client.GetS100ProductsFromSpecificDateAsync(sinceDate, _job);

            Assert.Multiple(() =>
            {
                Assert.That(result.ResponseCode, Is.EqualTo(HttpStatusCode.NotModified));
                Assert.That(lastModified?.ToUniversalTime(), Is.EqualTo(TruncateToSeconds(expectedLastModifiedUtc)));
            });
        }

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncThrowsApiExceptionWithUnexpectedStatus_ThenReturnsEmptyResponseAndLogError()
        {
            var apiException = new ApiException("Bad Request") { ResponseStatusCode = 400 };
            A.CallTo(() => _adapter.GetBasicCatalogueAsync(A<DateTime?>._, _job, A<HeadersInspectionHandlerOption>._, A<CancellationToken>._))
                .Throws(apiException);

            var sinceDate = DateTime.UtcNow;
            var (result, _) = await _client.GetS100ProductsFromSpecificDateAsync(sinceDate, _job);

            A.CallTo(() => _logger.Log<LoggerMessageState>(
            LogLevel.Error,
            A<EventId>.That.Matches(e => e.Name == "SalesCatalogueUnexpectedStatusCode"),
            A<LoggerMessageState>._,
            null,
            A<Func<LoggerMessageState, Exception?, string>>._))
            .MustHaveHappenedOnceExactly();
            Assert.That(result.ResponseCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncReceivesNullResult_ThenReturnsEmptyResponseAndLogError()
        {
            A.CallTo(() => _adapter.GetBasicCatalogueAsync(
            A<DateTime?>._,
            A<Job>._,
            A<HeadersInspectionHandlerOption>._,
            A<CancellationToken>._))
            .Returns(Task.FromResult<List<S100BasicCatalogue>?>(null));

            var sinceDate = DateTime.UtcNow;
            var (result, lastModified) = await _client.GetS100ProductsFromSpecificDateAsync(sinceDate, _job);

            Assert.Multiple(() =>
            {
                Assert.That(result.ResponseBody == null || result.ResponseBody.Count == 0, Is.True, "ResponseBody should be null or empty when API returns null result.");
                Assert.That(result.ResponseCode, Is.EqualTo(HttpStatusCode.InternalServerError).Or.EqualTo(default(HttpStatusCode)), "ResponseCode should indicate error or default value.");
                Assert.That(lastModified, Is.EqualTo(sinceDate).Within(TimeSpan.FromSeconds(1)), "LastModified should match the input sinceDate.");
            });

            A.CallTo(() => _logger.Log<LoggerMessageState>(
            LogLevel.Error,
            A<EventId>.That.Matches(e => e.Name == "SalesCatalogueError"),
            A<LoggerMessageState>._,
            null,
            A<Func<LoggerMessageState, Exception?, string>>._))
            .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenPostProductNamesAsyncReceivesSuccessWithValidProducts_ThenReturnsProductNamesWithOkResponse()
        {
            var productNames = new List<string> { "ProductA", "ProductB" };
            var productResponse = new S100ProductResponse
            {
                Products = new List<ProductNames>
                {
                    new ProductNames { ProductName = "ProductA", EditionNumber = 1 }
                }
            };
            A.CallTo(() => _adapter.PostProductNamesAsync(productNames, _job, A<CancellationToken>._))
            .Returns(Task.FromResult<S100ProductResponse?>(productResponse));

            var result = await _client.GetS100ProductNamesAsync(productNames, _job, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.Products.Count, Is.EqualTo(1));
                Assert.That(result.Products[0].ProductName, Is.EqualTo("ProductA"));
                Assert.That(result.ResponseCode, Is.EqualTo(HttpStatusCode.OK));
            });
        }

        [Test]
        public async Task WhenGetS100ProductNamesAsyncThrowsApiException_ThenGetS100ProductNamesAsyncReturnsEmptyResponseAndLogError()
        {
            var productNames = new List<string> { "ProductA" };
            var apiException = new ApiException("Bad Request");

            A.CallTo(() => _adapter.PostProductNamesAsync(productNames, _job, A<CancellationToken>._))
                .Throws(apiException);

            var result = await _client.GetS100ProductNamesAsync(productNames, _job, CancellationToken.None);

            A.CallTo(() => _logger.Log<LoggerMessageState>(
            LogLevel.Error,
            A<EventId>.That.Matches(e => e.Name == "SalesCatalogueUnexpectedStatusCode"),
            A<LoggerMessageState>._,
            null,
            A<Func<LoggerMessageState, Exception?, string>>._))
            .MustHaveHappenedOnceExactly();
            Assert.Multiple(() =>
            {
                Assert.That(result.Products, Is.Empty);
                Assert.That(result.ResponseCode, Is.EqualTo(default(HttpStatusCode)));
            });
        }

        [Test]
        public async Task WhenGetS100ProductNamesAsyncReceivesNullResponse_ThenReturnsEmptyProductsAndLogError()
        {
            var productNames = new List<string> { "ProductA" };
            A.CallTo(() => _adapter.PostProductNamesAsync(productNames, _job, A<CancellationToken>._))
            .Returns(Task.FromResult<S100ProductResponse?>(null));

            var result = await _client.GetS100ProductNamesAsync(productNames, _job, CancellationToken.None);

            A.CallTo(() => _logger.Log<LoggerMessageState>(
            LogLevel.Error,
            A<EventId>.That.Matches(e => e.Name == "SalesCatalogueError"),
            A<LoggerMessageState>._,
            null,
            A<Func<LoggerMessageState, Exception?, string>>._))
            .MustHaveHappenedOnceExactly();
            Assert.That(result.Products, Is.Empty);
        }

        [Test]
        public async Task WhenGetS100ProductNamesAsyncReceivesProductNamesAsEmpty_ThenReturnsEmptyProducts()
        {
            var productNames = new List<string>();
            var productResponse = new S100ProductResponse
            {
                Products = []
            };
            A.CallTo(() => _adapter.PostProductNamesAsync(productNames, _job, A<CancellationToken>._))
             .Returns(Task.FromResult<S100ProductResponse?>(productResponse));

            var result = await _client.GetS100ProductNamesAsync(productNames, _job, CancellationToken.None);

            Assert.That(result.Products, Is.Empty);
        }

        private static DateTime TruncateToSeconds(DateTime dateTime) => new(dateTime.Ticks - (dateTime.Ticks % TimeSpan.TicksPerSecond), dateTime.Kind);
    }
}
