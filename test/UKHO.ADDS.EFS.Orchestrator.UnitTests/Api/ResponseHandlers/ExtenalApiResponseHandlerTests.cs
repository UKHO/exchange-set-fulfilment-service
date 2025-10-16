using System.Net;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Domain.Constants;
using UKHO.ADDS.EFS.Domain.Files;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api.Models;
using UKHO.ADDS.EFS.Orchestrator.Api.ResponseHandlers;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Api.ResponseHandlers
{
    public class ExtenalApiResponseHandlerTests
    {
        private const string R = "R";
        private const string DummyJobId = "dummy-job-id";
        private const string DummyBatchId = "dummy-batch-id";
        private ExternalApiResponseHandler _handler = null!;
        private ILoggerFactory _loggerFactory = null!;
        private ILogger _logger = null!;
        private DefaultHttpContext _httpContext = null!;

        [SetUp]
        public void SetUp()
        {
            _handler = new ExternalApiResponseHandler();
            _logger = A.Fake<ILogger>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            A.CallTo(() => _loggerFactory.CreateLogger("S100ExchangeSetApi")).Returns(_logger);

            _httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceCollection()
                    .AddLogging()
                    .BuildServiceProvider()
            };
        }

        [TearDown]
        public void TearDown() => _loggerFactory?.Dispose();

        [Test]
        public async Task WhenUstreamApiResponseIsNull_ThenHandlerReturns500()
        {
            var result = _handler.HandleExternalApiResponse(null!, "ProductNames", _logger, _httpContext);

            await ExecuteAndAssertStatusAsync(result, _httpContext, StatusCodes.Status500InternalServerError);
        }

        [Test]
        public async Task WhenExternalApiResponseIsOk_ThenHandlerReturns202()
        {
            var now = DateTime.UtcNow;
            var response = NewPipelineResponse(
                externalApiResponseCode: HttpStatusCode.OK,
                externalApiLastModified: now,
                externalApiServiceName: ServiceNameType.FSS,
                buildStatus: Domain.Builds.BuildState.Scheduled,
                responseModel: NewExchangeSetResponse(now.AddHours(1), BatchId.From(DummyBatchId))
            );

            var result = _handler.HandleExternalApiResponse(response, "ProductNames", _logger, _httpContext);

            await ExecuteAndAssertStatusAsync(result, _httpContext, StatusCodes.Status202Accepted);
            Assert.That(_httpContext.Response.Headers.ContainsKey(ApiHeaderKeys.LastModifiedHeaderKey), Is.True);
            Assert.That(_httpContext.Response.Headers[ApiHeaderKeys.LastModifiedHeaderKey], Is.EqualTo(now.ToUniversalTime().ToString(R)));
        }

        [Test]
        public async Task WhenExternalApiResponseIsProductVersionReturnNotModified_ThenHanlerReturns202()
        {
            var lastModified = DateTime.UtcNow.AddHours(-1);
            var response = NewPipelineResponse(
                externalApiResponseCode: HttpStatusCode.NotModified,
                externalApiLastModified: lastModified,
                externalApiServiceName: ServiceNameType.SCS,
                buildStatus: Domain.Builds.BuildState.Scheduled,
                responseModel: NewExchangeSetResponse(lastModified.AddHours(1), BatchId.From(DummyBatchId))
            );

            var result = _handler.HandleExternalApiResponse(response, "ProductVersion", _logger, _httpContext);

            await ExecuteAndAssertStatusAsync(result, _httpContext, StatusCodes.Status202Accepted);
            Assert.That(_httpContext.Response.Headers.ContainsKey(ApiHeaderKeys.LastModifiedHeaderKey), Is.True);
            Assert.That(_httpContext.Response.Headers[ApiHeaderKeys.LastModifiedHeaderKey], Is.EqualTo(lastModified.ToUniversalTime().ToString(R)));
        }

        [Test]
        public async Task WhenExternalApiResponseIsNotModified_ThenHandlerReturns304AndAppendsLastModified()
        {
            var lastModified = DateTime.UtcNow.AddDays(-1);
            var response = NewPipelineResponse(
                externalApiResponseCode: HttpStatusCode.NotModified,
                externalApiLastModified: lastModified,
                externalApiServiceName: ServiceNameType.FSS,
                buildStatus: Domain.Builds.BuildState.NotScheduled,
                responseModel: NewExchangeSetResponse(lastModified.AddHours(1), BatchId.None)
            );

            var result = _handler.HandleExternalApiResponse(response, "UpdatesSince", _logger, _httpContext);

            await ExecuteAndAssertStatusAsync(result, _httpContext, StatusCodes.Status304NotModified);
            Assert.That(_httpContext.Response.Headers.ContainsKey(ApiHeaderKeys.LastModifiedHeaderKey), Is.True);
            Assert.That(_httpContext.Response.Headers[ApiHeaderKeys.LastModifiedHeaderKey], Is.EqualTo(lastModified.ToUniversalTime().ToString(R)));
        }

        [TestCase(HttpStatusCode.BadRequest, ServiceNameType.SCS)]
        [TestCase(HttpStatusCode.Unauthorized, ServiceNameType.SCS)]
        [TestCase(HttpStatusCode.Forbidden, ServiceNameType.FSS)]
        [TestCase(HttpStatusCode.UnsupportedMediaType, ServiceNameType.FSS)]
        [TestCase(HttpStatusCode.InternalServerError, ServiceNameType.FSS)]
        public async Task WhenExternalApiResponseIsReturnErrorStatus_ThenHandlerAppendsErrorOriginHeadersAndReturns500(HttpStatusCode externalApiStatus, ServiceNameType externalApiServiceName)
        {
            var response = NewPipelineResponse(
                externalApiResponseCode: externalApiStatus,
                externalApiLastModified: null,
                externalApiServiceName: externalApiServiceName,
                buildStatus: Domain.Builds.BuildState.NotScheduled,
                responseModel: NewExchangeSetResponse(DateTime.UtcNow.AddHours(1), BatchId.None)
            );

            var result = _handler.HandleExternalApiResponse(response, "ProductVersions", _logger, _httpContext);

            await ExecuteAndAssertStatusAsync(result, _httpContext, StatusCodes.Status500InternalServerError);
            Assert.That(_httpContext.Response.Headers.ContainsKey(ApiHeaderKeys.ErrorOriginHeaderKey), Is.True);
            Assert.That(_httpContext.Response.Headers.ContainsKey(ApiHeaderKeys.ErrorOriginStatusHeaderKey), Is.True);
            Assert.That(_httpContext.Response.Headers[ApiHeaderKeys.ErrorOriginHeaderKey], Is.EqualTo(externalApiServiceName.ToString()));
            Assert.That(_httpContext.Response.Headers[ApiHeaderKeys.ErrorOriginStatusHeaderKey], Is.EqualTo(((int)externalApiStatus).ToString()));
        }

        // Helpers to reduce duplication
        private AssemblyPipelineResponse NewPipelineResponse(HttpStatusCode externalApiResponseCode = HttpStatusCode.OK,
            DateTime? externalApiLastModified = null, Domain.Builds.BuildState buildStatus = Domain.Builds.BuildState.NotScheduled,
            ServiceNameType externalApiServiceName = ServiceNameType.SCS,
            CustomExchangeSetResponse? responseModel = null)
        {
            return new AssemblyPipelineResponse
            {
                JobId = JobId.From(DummyJobId),
                JobStatus = JobState.Submitted,
                DataStandard = DataStandard.S100,
                BatchId = BatchId.From(DummyBatchId),
                ExternalApiResponseCode = externalApiResponseCode,
                ExternalApiLastModified = externalApiLastModified,
                ExternalApiServiceName = externalApiServiceName,
                BuildStatus = buildStatus,
                Response = responseModel
            };
        }

        private static CustomExchangeSetResponse NewExchangeSetResponse(DateTime expiry, BatchId batchId)
        {
            return new CustomExchangeSetResponse
            {
                Links = new ExchangeSetLinks
                {
                    ExchangeSetBatchStatusUri = new Link { Uri = new Uri("https://dummy-status-uri") },
                    ExchangeSetBatchDetailsUri = new Link { Uri = new Uri("https://dummy-details-uri") }
                },
                FssBatchId = batchId,
                RequestedProductCount = ProductCount.From(1),
                ExchangeSetProductCount = ProductCount.From(1),
                RequestedProductsAlreadyUpToDateCount = ProductCount.From(0),
                RequestedProductsNotInExchangeSet = [],
                ExchangeSetUrlExpiryDateTime = expiry
            };
        }

        private static async Task ExecuteAndAssertStatusAsync(IResult result, HttpContext httpContext, int expectedStatus)
        {
            await result.ExecuteAsync(httpContext);
            Assert.That(httpContext.Response.StatusCode, Is.EqualTo(expectedStatus));
        }
    }
}
