using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.SalesCatalogueService;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Tests.Services
{
    [TestFixture]
    public class SalesCatalogueServiceTests
    {
        private ISalesCatalogueClient _fakeSalesCatalogueClient;
        private ILogger<SalesCatalogueService> _logger;
        private SalesCatalogueService _salesCatalogueService;
        private ExchangeSetJob _exchangeSetJob;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _fakeSalesCatalogueClient = A.Fake<ISalesCatalogueClient>();
            _logger = A.Fake<ILogger<SalesCatalogueService>>();
            _salesCatalogueService = new SalesCatalogueService(_fakeSalesCatalogueClient, _logger);
            _exchangeSetJob = new TestExchangeSetJob
            {
                Id = "test-correlation-id"
            };
        }

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncReturnsOK_ThenReturnsDataAndLastModified()
        {
            var expectedResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                LastModified = DateTime.UtcNow
            };
            var expectedDate = expectedResponse.LastModified;

            var successResult = A.Fake<IResult<S100SalesCatalogueResponse>>();
            S100SalesCatalogueResponse outResponse;
            IError error = null;
            A.CallTo(() => successResult.IsSuccess(out expectedResponse, out error)).Returns(true);
            A.CallTo(() => _fakeSalesCatalogueClient.GetS100ProductsFromSpecificDateAsync(
                A<string>.Ignored, A<string>.Ignored, A<DateTime?>.Ignored, A<string>.Ignored))
                .Returns(Task.FromResult(successResult));
            A.CallTo(() => successResult.IsSuccess(out expectedResponse, out error)).Returns(true);

            var result = await _salesCatalogueService.GetS100ProductsFromSpecificDateAsync(null, _exchangeSetJob);
            S100SalesCatalogueResponse s100SalesCatalogueData = result.s100SalesCatalogueData;
            DateTime? lastModified = result.LastModified; 

            Assert.Multiple(() =>
            {
                Assert.That(s100SalesCatalogueData.ResponseCode, Is.EqualTo(expectedResponse.ResponseCode));
                Assert.That(s100SalesCatalogueData.LastModified, Is.EqualTo(expectedResponse.LastModified));
                Assert.That(lastModified, Is.EqualTo(expectedDate));
            });
        }

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncReturnsNotModified_ThenReturnsDataAndLastModified()
        {
            var sinceDateTime = DateTime.UtcNow.AddDays(-1);
            var expectedResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.NotModified,
                LastModified = sinceDateTime
            };

            var successResult = A.Fake<IResult<S100SalesCatalogueResponse>>();
            S100SalesCatalogueResponse outResponse;
            IError error = null;
            A.CallTo(() => successResult.IsSuccess(out expectedResponse, out error)).Returns(true);
            A.CallTo(() => _fakeSalesCatalogueClient.GetS100ProductsFromSpecificDateAsync(
                    A<string>.Ignored, A<string>.Ignored, sinceDateTime, A<string>.Ignored))
                .Returns(Task.FromResult(successResult));

            var result = await _salesCatalogueService.GetS100ProductsFromSpecificDateAsync(sinceDateTime, _exchangeSetJob);

            S100SalesCatalogueResponse data = result.s100SalesCatalogueData;

            Assert.Multiple(() =>
            {
                Assert.That(data.ResponseCode, Is.EqualTo(expectedResponse.ResponseCode));
                Assert.That(result.LastModified, Is.EqualTo(sinceDateTime));
            });
        }

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncIsNotSuccess_ThenLogsErrorAndReturnsDefault()
        {
            var error = A.Fake<IError>();
            var failResult = A.Fake<IResult<S100SalesCatalogueResponse>>();
            S100SalesCatalogueResponse? outResponse = null;
            A.CallTo(() => failResult.IsSuccess(out outResponse!, out error)).Returns(false);
            A.CallTo(() => _fakeSalesCatalogueClient.GetS100ProductsFromSpecificDateAsync(
                    A<string>.Ignored, A<string>.Ignored, A<DateTime?>.Ignored, A<string>.Ignored))
                .Returns(Task.FromResult(failResult));

            var result = await _salesCatalogueService.GetS100ProductsFromSpecificDateAsync(null, _exchangeSetJob);

            A.CallTo(() => _logger.Log(A<LogLevel>._, A<EventId>._, A<LoggerMessageState>._, A<Exception>._, A<Func<LoggerMessageState, Exception?, string>>._)).MustHaveHappened();
            S100SalesCatalogueResponse data = result.s100SalesCatalogueData;
            Assert.Multiple(() =>
            {
                Assert.That(data, Is.Not.Null);
                Assert.That(data.LastModified, Is.Null);
            });
        }

        [Test]
        public async Task WhenSinceDateTimeIsNull_ThenReturnsDataAndLastModified()
        {
            var expectedResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                LastModified = DateTime.UtcNow
            };
            var successResult = A.Fake<IResult<S100SalesCatalogueResponse>>();
            S100SalesCatalogueResponse outResponse;
            IError error = null;
            A.CallTo(() => successResult.IsSuccess(out expectedResponse, out error)).Returns(true);
            A.CallTo(() => _fakeSalesCatalogueClient.GetS100ProductsFromSpecificDateAsync(
                    A<string>.Ignored, A<string>.Ignored, null, A<string>.Ignored))
                .Returns(Task.FromResult(successResult));

            var result = await _salesCatalogueService.GetS100ProductsFromSpecificDateAsync(null, _exchangeSetJob);
            S100SalesCatalogueResponse data = result.s100SalesCatalogueData;

            Assert.Multiple(() =>
            {
                Assert.That(data.ResponseCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(data.LastModified, Is.EqualTo(expectedResponse.LastModified));
            });
        }

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncReturnsUnexpectedStatusCode_ThenLogsWarningAndReturnsDefault()
        {
            var expectedResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.BadRequest, // unexpected status code
                LastModified = null
            };

            var successResult = A.Fake<IResult<S100SalesCatalogueResponse>>();
            S100SalesCatalogueResponse outResponse;
            IError error = null;
            A.CallTo(() => successResult.IsSuccess(out expectedResponse, out error)).Returns(true);
            A.CallTo(() => _fakeSalesCatalogueClient.GetS100ProductsFromSpecificDateAsync(
                    A<string>.Ignored, A<string>.Ignored, A<DateTime?>.Ignored, A<string>.Ignored))
                .Returns(Task.FromResult(successResult));

            var result = await _salesCatalogueService.GetS100ProductsFromSpecificDateAsync(null, _exchangeSetJob);
            S100SalesCatalogueResponse data = result.s100SalesCatalogueData;
            A.CallTo(() => _logger.Log(A<LogLevel>._, A<EventId>._, A<LoggerMessageState>._, A<Exception>._, A<Func<LoggerMessageState, Exception?, string>>._)).MustHaveHappened();

            Assert.Multiple(() =>
            {
                Assert.That(data, Is.Not.Null);
                Assert.That(data.ResponseCode, Is.EqualTo(default(HttpStatusCode)));
                Assert.That(data.LastModified, Is.Null);
            });
        }

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncFails_ThenLogSalesCatalogueErrorIsCalled()
        {
            var error = A.Fake<IError>();
            var failResult = A.Fake<IResult<S100SalesCatalogueResponse>>();
            S100SalesCatalogueResponse? outResponse = null;
            A.CallTo(() => failResult.IsSuccess(out outResponse!, out error)).Returns(false);
            A.CallTo(() => _fakeSalesCatalogueClient.GetS100ProductsFromSpecificDateAsync(A<string>.Ignored, A<string>.Ignored, A<DateTime?>.Ignored, A<string>.Ignored)).Returns(Task.FromResult(failResult));

            var result = await _salesCatalogueService.GetS100ProductsFromSpecificDateAsync(null, _exchangeSetJob);
            S100SalesCatalogueResponse data = result.s100SalesCatalogueData;

            A.CallTo(() => _logger.Log(A<LogLevel>._, A<EventId>._, A<LoggerMessageState>._, A<Exception>._, A<Func<LoggerMessageState, Exception?, string>>._)).MustHaveHappened();

            Assert.Multiple(() =>
            {
                Assert.That(data, Is.Not.Null);
                Assert.That(result.LastModified, Is.Null);
            });
        }

        [Test]
        public async Task WhenTransientFailureOccurs_RetryPolicyRetriesExpectedNumberOfTimes()
        {
            var expectedResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                LastModified = DateTime.UtcNow
            };

            int callCount = 0;
            var retriableError = new UKHO.ADDS.Infrastructure.Results.Error(
                "Retriable error",
                new Dictionary<string, object> { { "StatusCode", 503 } }
            );

            var successResult = A.Fake<IResult<S100SalesCatalogueResponse>>();
            IError? successError = null;
            A.CallTo(() => successResult.IsSuccess(out expectedResponse, out successError)).Returns(true);

            A.CallTo(() => _fakeSalesCatalogueClient.GetS100ProductsFromSpecificDateAsync(
                A<string>.Ignored, A<string>.Ignored, A<DateTime?>.Ignored, A<string>.Ignored))
                .ReturnsLazily(() =>
                {
                    callCount++;
                    if (callCount < 4)
                    {
                        return (Result.Failure<S100SalesCatalogueResponse>(retriableError));
                    }
                    return successResult;
                });

            var result = await _salesCatalogueService.GetS100ProductsFromSpecificDateAsync(null, _exchangeSetJob);
            S100SalesCatalogueResponse data = result.s100SalesCatalogueData;
            Assert.Multiple(() =>
            {
                Assert.That(callCount, Is.EqualTo(4), "Should retry 3 times plus the initial call (total 4)");
                Assert.That(data.ResponseCode, Is.EqualTo(expectedResponse.ResponseCode));
                Assert.That(result.LastModified, Is.EqualTo(expectedResponse.LastModified));
            });
        }
    }
    public class TestExchangeSetJob : ExchangeSetJob
    {
        public override string GetProductDelimitedList() => "test-products";
        public override int GetProductCount() => 1;
    }
}
