using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.SalesCatalogueService;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Services.Infrastructure
{
    [TestFixture]
    public class OrchestratorSalesCatalogueClientTest
    {
        private ISalesCatalogueClient _salesCatalogueClient;
        private ILogger<OrchestratorSalesCatalogueClient> _logger;
        private OrchestratorSalesCatalogueClient _orchestratorSalesCatalogueClient;
        private Job _job;

        [SetUp]
        public void Setup()
        {
            _salesCatalogueClient = A.Fake<ISalesCatalogueClient>();
            _logger = A.Fake<ILogger<OrchestratorSalesCatalogueClient>>();
            _orchestratorSalesCatalogueClient = new OrchestratorSalesCatalogueClient(_salesCatalogueClient, _logger);

            _job = new Job
            {
                Id = "test-job-id",
                Timestamp = DateTime.UtcNow,
                DataStandard = DataStandard.S100,
                RequestedProducts = "products",
                RequestedFilter = "filter",
                DataStandardTimestamp = DateTime.UtcNow.AddDays(-1)
            };
        }

        [Test]
        public void WhenConstructorCalledWithNullSalesCatalogueClient_ThenThrowsArgumentNullException()
        {
            Assert.That(() => new OrchestratorSalesCatalogueClient(null, _logger),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("salesCatalogueClient"));
        }

        [Test]
        public void WhenConstructorCalledWithNullLogger_ThenThrowsArgumentNullException()
        {
            Assert.That(() => new OrchestratorSalesCatalogueClient(_salesCatalogueClient, null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("logger"));
        }

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncCalledAndApiReturnsOK_ThenReturnsDataWithApiLastModifiedTimestamp()
        {
            var sinceDateTime = DateTime.UtcNow.AddDays(-7);
            var lastModifiedTime = DateTime.UtcNow;
            var apiResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                LastModified = lastModifiedTime
            };

            var result = Result.Success(apiResponse);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(
                "v2", "s100", sinceDateTime, _job.Id))
                .Returns(Task.FromResult<IResult<S100SalesCatalogueResponse>>(result));

            var (s100SalesCatalogueData, lastModified) = await _orchestratorSalesCatalogueClient.GetS100ProductsFromSpecificDateAsync(sinceDateTime, _job);

            Assert.That(s100SalesCatalogueData, Is.SameAs(apiResponse));
            Assert.That(lastModified, Is.EqualTo(lastModifiedTime));
        }

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncCalledAndApiReturnsNotModified_ThenReturnsDataWithOriginalTimestamp()
        {
            var sinceDateTime = DateTime.UtcNow.AddDays(-7);
            var apiResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.NotModified
            };

            var result = Result.Success(apiResponse);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(
                "v2", "s100", sinceDateTime, _job.Id))
                .Returns(Task.FromResult<IResult<S100SalesCatalogueResponse>>(result));

            var (s100SalesCatalogueData, lastModified) = await _orchestratorSalesCatalogueClient.GetS100ProductsFromSpecificDateAsync(sinceDateTime, _job);

            Assert.That(s100SalesCatalogueData, Is.SameAs(apiResponse));
            Assert.That(lastModified, Is.EqualTo(sinceDateTime));
        }

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncCalledAndApiReturnsUnexpectedStatusCode_ThenReturnsEmptyResponseWithOriginalTimestamp()
        {
            var sinceDateTime = DateTime.UtcNow.AddDays(-7);
            var apiResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.BadRequest
            };

            var result = Result.Success(apiResponse);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(
                "v2", "s100", sinceDateTime, _job.Id))
                .Returns(Task.FromResult<IResult<S100SalesCatalogueResponse>>(result));

            var (s100SalesCatalogueData, lastModified) = await _orchestratorSalesCatalogueClient.GetS100ProductsFromSpecificDateAsync(sinceDateTime, _job);

            Assert.That(s100SalesCatalogueData, Is.Not.SameAs(apiResponse));
            Assert.That(s100SalesCatalogueData, Is.TypeOf<S100SalesCatalogueResponse>());
            Assert.That(lastModified, Is.EqualTo(sinceDateTime));
        }

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncCalledAndApiCallFails_ThenReturnsEmptyResponseWithOriginalTimestamp()
        {
            var sinceDateTime = DateTime.UtcNow.AddDays(-7);
            var apiError = A.Fake<IError>();
            var result = Result.Failure<S100SalesCatalogueResponse>(apiError);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(
                "v2", "s100", sinceDateTime, _job.Id))
                .Returns(Task.FromResult<IResult<S100SalesCatalogueResponse>>(result));

            var (s100SalesCatalogueData, lastModified) = await _orchestratorSalesCatalogueClient.GetS100ProductsFromSpecificDateAsync(sinceDateTime, _job);

            Assert.That(s100SalesCatalogueData, Is.TypeOf<S100SalesCatalogueResponse>());
            Assert.That(lastModified, Is.EqualTo(sinceDateTime));
        }

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncCalledWithNullSinceDateTime_ThenPassesNullToClient()
        {
            DateTime? sinceDateTime = null;
            var apiResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                LastModified = DateTime.UtcNow
            };

            var result = Result.Success(apiResponse);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(
                "v2", "s100", null, _job.Id))
                .Returns(Task.FromResult<IResult<S100SalesCatalogueResponse>>(result));

            await _orchestratorSalesCatalogueClient.GetS100ProductsFromSpecificDateAsync(sinceDateTime, _job);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(
                "v2", "s100", null, _job.Id))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenGetS100ProductNamesAsyncCalledAndApiReturnsOK_ThenReturnsDataWithJobTimestamp()
        {
            var productNames = new[] { "Product1", "Product2" };
            var jobTimestamp = _job.DataStandardTimestamp;
            var apiResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.OK,
                Products = new List<S100ProductNames> { new() { ProductName = "Product1" } }
            };

            var result = Result.Success(apiResponse);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                "v2", "s100", A<IEnumerable<string>>.That.IsSameSequenceAs(productNames), _job.Id, A<CancellationToken>._))
                .Returns(Task.FromResult<IResult<S100ProductNamesResponse>>(result));

            var s100SalesCatalogueData = await _orchestratorSalesCatalogueClient.GetS100ProductNamesAsync(
                productNames, _job, CancellationToken.None);

            Assert.That(s100SalesCatalogueData.Products, Is.EqualTo(apiResponse.Products));
        }

        [Test]
        public async Task WhenGetS100ProductNamesAsyncCalledAndApiReturnsNotModified_ThenReturnsDataWithJobTimestamp()
        {
            var productNames = new[] { "Product1", "Product2" };
            var jobTimestamp = _job.DataStandardTimestamp;
            var apiResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.NotModified
            };

            var result = Result.Success(apiResponse);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                "v2", "s100", A<IEnumerable<string>>.That.IsSameSequenceAs(productNames), _job.Id, A<CancellationToken>._))
                .Returns(Task.FromResult<IResult<S100ProductNamesResponse>>(result));

            var s100SalesCatalogueData = await _orchestratorSalesCatalogueClient.GetS100ProductNamesAsync(
                productNames, _job, CancellationToken.None);

           Assert.That(s100SalesCatalogueData.Products, Is.EqualTo(apiResponse.Products));
        }

        [Test]
        public async Task WhenGetS100ProductNamesAsyncCalledAndApiReturnsUnexpectedStatusCode_ThenReturnsEmptyResponseWithJobTimestamp()
        {
            var productNames = new[] { "Product1", "Product2" };
            var jobTimestamp = _job.DataStandardTimestamp;
            var apiResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.BadRequest
            };

            var result = Result.Success(apiResponse);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                "v2", "s100", A<IEnumerable<string>>.That.IsSameSequenceAs(productNames), _job.Id, A<CancellationToken>._))
                .Returns(Task.FromResult<IResult<S100ProductNamesResponse>>(result));

            var s100SalesCatalogueData = await _orchestratorSalesCatalogueClient.GetS100ProductNamesAsync(
                productNames, _job, CancellationToken.None);

            Assert.That(s100SalesCatalogueData, Is.Not.SameAs(apiResponse));
            Assert.That(s100SalesCatalogueData, Is.TypeOf<S100ProductNamesResponse>());
        }

        [Test]
        public async Task WhenGetS100ProductNamesAsyncCalledAndApiCallFails_ThenReturnsEmptyResponseWithJobTimestamp()
        {
            var productNames = new[] { "Product1", "Product2" };
            var jobTimestamp = _job.DataStandardTimestamp;
            var apiError = A.Fake<IError>();
            var result = Result.Failure<S100ProductNamesResponse>(apiError);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                "v2", "s100", A<IEnumerable<string>>.That.IsSameSequenceAs(productNames), _job.Id, A<CancellationToken>._))
                .Returns(Task.FromResult<IResult<S100ProductNamesResponse>>(result));

            var s100SalesCatalogueData = await _orchestratorSalesCatalogueClient.GetS100ProductNamesAsync(
                productNames, _job, CancellationToken.None);

            Assert.That(s100SalesCatalogueData, Is.TypeOf<S100ProductNamesResponse>());
        }

        [Test]
        public async Task WhenGetS100ProductNamesAsyncCalledWithEmptyProductNames_ThenPassesEmptyListToClient()
        {
            var productNames = Array.Empty<string>();
            var apiResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.OK
            };

            var result = Result.Success(apiResponse);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                "v2", "s100", A<IEnumerable<string>>.That.IsEmpty(), _job.Id, A<CancellationToken>._))
                .Returns(Task.FromResult<IResult<S100ProductNamesResponse>>(result));

            await _orchestratorSalesCatalogueClient.GetS100ProductNamesAsync(
                productNames, _job, CancellationToken.None);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                "v2", "s100", A<IEnumerable<string>>.That.IsEmpty(), _job.Id, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenGetS100ProductNamesAsyncCalledWithCancellationToken_ThenPassesTokenToClient()
        {
            var productNames = new[] { "Product1" };
            var cancellationToken = new CancellationToken(true);
            var apiResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.OK
            };

            var result = Result.Success(apiResponse);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                "v2", "s100", A<IEnumerable<string>>._, _job.Id, cancellationToken))
                .Returns(Task.FromResult<IResult<S100ProductNamesResponse>>(result));

            await _orchestratorSalesCatalogueClient.GetS100ProductNamesAsync(
                productNames, _job, cancellationToken);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                "v2", "s100", A<IEnumerable<string>>._, _job.Id, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncCalled_ThenUsesRetryPolicy()
        {
            var sinceDateTime = DateTime.UtcNow.AddDays(-7);
            var apiResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                LastModified = DateTime.UtcNow
            };

            var result = Result.Success(apiResponse);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(
                "v2", "s100", sinceDateTime, _job.Id))
                .Returns(Task.FromResult<IResult<S100SalesCatalogueResponse>>(result));

            await _orchestratorSalesCatalogueClient.GetS100ProductsFromSpecificDateAsync(sinceDateTime, _job);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(
                "v2", "s100", sinceDateTime, _job.Id))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenGetS100ProductNamesAsyncCalled_ThenUsesRetryPolicy()
        {
            var productNames = new[] { "Product1" };
            var apiResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.OK
            };

            var result = Result.Success(apiResponse);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                "v2", "s100", A<IEnumerable<string>>._, _job.Id, A<CancellationToken>._))
                .Returns(Task.FromResult<IResult<S100ProductNamesResponse>>(result));


            await _orchestratorSalesCatalogueClient.GetS100ProductNamesAsync(
                productNames, _job, CancellationToken.None);

            A.CallTo(() => _salesCatalogueClient.GetS100ProductNamesAsync(
                "v2", "s100", A<IEnumerable<string>>._, _job.Id, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }
    }
}
