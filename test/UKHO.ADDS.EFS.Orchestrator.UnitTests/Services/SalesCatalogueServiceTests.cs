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

        [SetUp]
        public void SetUp()
        {
            _fakeSalesCatalogueClient = A.Fake<ISalesCatalogueClient>();
            _logger = A.Fake<ILogger<SalesCatalogueService>>();
            _salesCatalogueService = new SalesCatalogueService(_fakeSalesCatalogueClient, _logger);
            _exchangeSetJob = new TestExchangeSetJob
            {
                Id = "test-correlation-id",
                SalesCatalogueTimestamp = DateTime.UtcNow.AddDays(-7)
            };
        }

        #region Constructor Tests

        [Test]
        public void WhenSalesCatalogueClientIsNull_ThenThrowsArgumentNullException()
        {
            Assert.That(() => new SalesCatalogueService(null, _logger),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("salesCatalogueClient"));
        }

        [Test]
        public void WhenLoggerIsNull_ThenThrowsArgumentNullException()
        {
            Assert.That(() => new SalesCatalogueService(_fakeSalesCatalogueClient, null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("logger"));
        }

        [Test]
        public void WhenValidParametersProvided_ThenConstructsSuccessfully()
        {
            var service = new SalesCatalogueService(_fakeSalesCatalogueClient, _logger);
            Assert.That(service, Is.Not.Null);
        }

        #endregion

        #region GetS100ProductsFromSpecificDateAsync Tests

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncReturnsOK_ThenReturnsDataAndLastModified()
        {
            var expectedResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                LastModified = DateTime.UtcNow
            };

            SetupSalesCatalogueClientSuccess(expectedResponse);

            var result = await _salesCatalogueService.GetS100ProductsFromSpecificDateAsync(null, _exchangeSetJob);

            Assert.Multiple(() =>
            {
                Assert.That(result.s100SalesCatalogueData.ResponseCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result.s100SalesCatalogueData.LastModified, Is.EqualTo(expectedResponse.LastModified));
                Assert.That(result.LastModified, Is.EqualTo(expectedResponse.LastModified));
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

            SetupSalesCatalogueClientSuccess(expectedResponse, sinceDateTime);

            var result = await _salesCatalogueService.GetS100ProductsFromSpecificDateAsync(sinceDateTime, _exchangeSetJob);

            Assert.Multiple(() =>
            {
                Assert.That(result.s100SalesCatalogueData.ResponseCode, Is.EqualTo(HttpStatusCode.NotModified));
                Assert.That(result.LastModified, Is.EqualTo(sinceDateTime));
            });
        }

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncIsNotSuccess_ThenLogsErrorAndReturnsDefault()
        {
            SetupSalesCatalogueClientFailure<S100SalesCatalogueResponse>();

            var result = await _salesCatalogueService.GetS100ProductsFromSpecificDateAsync(null, _exchangeSetJob);

            AssertLoggerCalled();
            Assert.Multiple(() =>
            {
                Assert.That(result.s100SalesCatalogueData, Is.Not.Null);
                Assert.That(result.s100SalesCatalogueData.LastModified, Is.Null);
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

            SetupSalesCatalogueClientSuccess(expectedResponse, null);

            var result = await _salesCatalogueService.GetS100ProductsFromSpecificDateAsync(null, _exchangeSetJob);

            Assert.Multiple(() =>
            {
                Assert.That(result.s100SalesCatalogueData.ResponseCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result.s100SalesCatalogueData.LastModified, Is.EqualTo(expectedResponse.LastModified));
            });
        }

        [Test]
        public async Task WhenGetS100ProductsFromSpecificDateAsyncReturnsUnexpectedStatusCode_ThenLogsWarningAndReturnsDefault()
        {
            var expectedResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.BadRequest,
                LastModified = null
            };

            SetupSalesCatalogueClientSuccess(expectedResponse);

            var result = await _salesCatalogueService.GetS100ProductsFromSpecificDateAsync(null, _exchangeSetJob);

            AssertLoggerCalled();
            Assert.Multiple(() =>
            {
                Assert.That(result.s100SalesCatalogueData, Is.Not.Null);
                Assert.That(result.s100SalesCatalogueData.ResponseCode, Is.EqualTo(default(HttpStatusCode)));
                Assert.That(result.s100SalesCatalogueData.LastModified, Is.Null);
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
            var data = result.s100SalesCatalogueData;

            A.CallTo(() => _logger.Log(A<LogLevel>._, A<EventId>._, A<LoggerMessageState>._, A<Exception>._, A<Func<LoggerMessageState, Exception?, string>>._)).MustHaveHappened();

            Assert.Multiple(() =>
            {
                Assert.That(data, Is.Not.Null);
                Assert.That(result.LastModified, Is.Null);
            });
        }

        [Test]
        public async Task WhenTransientFailureOccursInProductsFromSpecificDate_RetryPolicyRetriesExpectedNumberOfTimes()
        {
            var expectedResponse = new S100SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                LastModified = DateTime.UtcNow
            };

            var callCount = 0;
            var retriableError = new Error(
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
            var data = result.s100SalesCatalogueData;
            Assert.Multiple(() =>
            {
                Assert.That(callCount, Is.EqualTo(4), "Should retry 3 times plus the initial call (total 4)");
                Assert.That(data.ResponseCode, Is.EqualTo(expectedResponse.ResponseCode));
                Assert.That(result.LastModified, Is.EqualTo(expectedResponse.LastModified));
            });
        }

        #endregion

        #region GetS100ProductNamesAsync Tests

        [Test]
        public async Task WhenGetS100ProductNamesAsyncReturnsOK_ThenReturnsDataAndSalesCatalogueTimestamp()
        {
            var productNames = new List<string> { "Product1", "Product2" };
            var expectedResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.OK,
                Products = new List<S100ProductNames>
                {
                    new S100ProductNames { ProductName = "Product1" },
                    new S100ProductNames { ProductName = "Product2" }
                }
            };

            SetupProductNamesClientSuccess(expectedResponse, productNames);

            var result = await _salesCatalogueService.GetS100ProductNamesAsync(productNames, _exchangeSetJob);

            Assert.Multiple(() =>
            {
                Assert.That(result.s100SalesCatalogueData.ResponseCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result.s100SalesCatalogueData.Products.Count, Is.EqualTo(2));
                Assert.That(result.LastModified, Is.EqualTo(_exchangeSetJob.SalesCatalogueTimestamp));
            });
        }

        [Test]
        public async Task WhenGetS100ProductNamesAsyncReturnsNotModified_ThenReturnsDataAndSalesCatalogueTimestamp()
        {
            var productNames = new List<string> { "Product1", "Product2" };
            var expectedResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.NotModified
            };

            SetupProductNamesClientSuccess(expectedResponse, productNames);

            var result = await _salesCatalogueService.GetS100ProductNamesAsync(productNames, _exchangeSetJob);

            Assert.Multiple(() =>
            {
                Assert.That(result.s100SalesCatalogueData.ResponseCode, Is.EqualTo(HttpStatusCode.NotModified));
                Assert.That(result.LastModified, Is.EqualTo(_exchangeSetJob.SalesCatalogueTimestamp));
            });
        }

        [Test]
        public async Task WhenGetS100ProductNamesAsyncReturnsUnexpectedStatusCode_ThenLogsWarningAndReturnsDefault()
        {
            var productNames = new List<string> { "Product1", "Product2" };
            var expectedResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.BadRequest
            };

            SetupProductNamesClientSuccess(expectedResponse, productNames);

            var result = await _salesCatalogueService.GetS100ProductNamesAsync(productNames, _exchangeSetJob);

            AssertLoggerCalled();
            Assert.Multiple(() =>
            {
                Assert.That(result.s100SalesCatalogueData, Is.Not.Null);
                Assert.That(result.s100SalesCatalogueData.ResponseCode, Is.EqualTo(default(HttpStatusCode)));
                Assert.That(result.LastModified, Is.EqualTo(_exchangeSetJob.SalesCatalogueTimestamp));
            });
        }

        [Test]
        public async Task WhenGetS100ProductNamesAsyncFails_ThenLogSalesCatalogueErrorIsCalled()
        {
            var productNames = new List<string> { "Product1", "Product2" };
            SetupProductNamesClientFailure(productNames);

            var result = await _salesCatalogueService.GetS100ProductNamesAsync(productNames, _exchangeSetJob);

            AssertLoggerCalled();
            Assert.Multiple(() =>
            {
                Assert.That(result.s100SalesCatalogueData, Is.Not.Null);
                Assert.That(result.LastModified, Is.EqualTo(_exchangeSetJob.SalesCatalogueTimestamp));
            });
        }

        [Test]
        public async Task WhenGetS100ProductNamesAsyncWithEmptyProductList_ThenClientIsCalledWithEmptyList()
        {
            var productNames = new List<string>();
            var expectedResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.OK,
                Products = new List<S100ProductNames>()
            };

            SetupProductNamesClientSuccess(expectedResponse, productNames);

            var result = await _salesCatalogueService.GetS100ProductNamesAsync(productNames, _exchangeSetJob);

            A.CallTo(() => _fakeSalesCatalogueClient.GetS100ProductNamesAsync(
                A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.That.IsEmpty(), A<string>.Ignored, A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();

            Assert.That(result.s100SalesCatalogueData.ResponseCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task WhenTransientFailureOccursInProductNames_RetryPolicyRetriesExpectedNumberOfTimes()
        {
            var productNames = new List<string> { "Product1", "Product2" };
            var expectedResponse = new S100ProductNamesResponse
            {
                ResponseCode = HttpStatusCode.OK,
                Products =
                [
                    new S100ProductNames() { ProductName = "Product1" },
                    new S100ProductNames() { ProductName = "Product2" }
                ]
            };

            var callCount = 0;
            var retriableError = new Error(
                "Retriable error",
                new Dictionary<string, object> { { "StatusCode", 503 } }
            );

            var successResult = A.Fake<IResult<S100ProductNamesResponse>>();
            IError? successError = null;
            A.CallTo(() => successResult.IsSuccess(out expectedResponse, out successError)).Returns(true);

            A.CallTo(() => _fakeSalesCatalogueClient.GetS100ProductNamesAsync(
                    A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .ReturnsLazily(() =>
                {
                    callCount++;
                    if (callCount < 4)
                    {
                        return (Result.Failure<S100ProductNamesResponse>(retriableError));
                    }
                    return successResult;
                });

            var result = await _salesCatalogueService.GetS100ProductNamesAsync(productNames, _exchangeSetJob);
            var data = result.s100SalesCatalogueData;

            Assert.Multiple(() =>
            {
                Assert.That(callCount, Is.EqualTo(4), "Should retry 3 times plus the initial call (total 4)");
                Assert.That(data.ResponseCode, Is.EqualTo(expectedResponse.ResponseCode));
                Assert.That(data.Products.Count, Is.EqualTo(2));
            });
        }

        #endregion

        #region Helpers

        private void SetupSalesCatalogueClientSuccess(S100SalesCatalogueResponse response, DateTime? sinceDateTime = null)
        {
            var successResult = A.Fake<IResult<S100SalesCatalogueResponse>>();
            IError error = null;
            A.CallTo(() => successResult.IsSuccess(out response, out error)).Returns(true);
            A.CallTo(() => _fakeSalesCatalogueClient.GetS100ProductsFromSpecificDateAsync(
                A<string>.Ignored, A<string>.Ignored, sinceDateTime, A<string>.Ignored))
                .Returns(Task.FromResult(successResult));
        }

        private void SetupSalesCatalogueClientFailure<T>() where T : class, new()
        {
            var error = A.Fake<IError>();
            var failResult = A.Fake<IResult<T>>();
            T? outResponse = null;
            A.CallTo(() => failResult.IsSuccess(out outResponse!, out error)).Returns(false);
            A.CallTo(() => _fakeSalesCatalogueClient.GetS100ProductsFromSpecificDateAsync(
                A<string>.Ignored, A<string>.Ignored, A<DateTime?>.Ignored, A<string>.Ignored))
                .Returns(Task.FromResult(failResult as IResult<S100SalesCatalogueResponse>));
        }

        private void SetupProductNamesClientSuccess(S100ProductNamesResponse response, IEnumerable<string> productNames)
        {
            var successResult = A.Fake<IResult<S100ProductNamesResponse>>();
            IError error = null;
            A.CallTo(() => successResult.IsSuccess(out response, out error)).Returns(true);
            A.CallTo(() => _fakeSalesCatalogueClient.GetS100ProductNamesAsync(
                A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.That.IsSameSequenceAs(productNames), A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(successResult));
        }

        private void SetupProductNamesClientFailure(IEnumerable<string> productNames)
        {
            var error = A.Fake<IError>();
            var failResult = A.Fake<IResult<S100ProductNamesResponse>>();
            S100ProductNamesResponse? outResponse = null;
            A.CallTo(() => failResult.IsSuccess(out outResponse!, out error)).Returns(false);
            A.CallTo(() => _fakeSalesCatalogueClient.GetS100ProductNamesAsync(
                A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.That.IsSameSequenceAs(productNames), A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(failResult));
        }

        private void AssertLoggerCalled()
        {
            A.CallTo(() => _logger.Log(
                A<LogLevel>._, A<EventId>._, A<LoggerMessageState>._, A<Exception>._, A<Func<LoggerMessageState, Exception?, string>>._))
                .MustHaveHappened();
        }

        #endregion

        public class TestExchangeSetJob : ExchangeSetJob
        {
            public override string GetProductDelimitedList() => "test-products";
            public override int GetProductCount() => 1;
        }
    }
}
