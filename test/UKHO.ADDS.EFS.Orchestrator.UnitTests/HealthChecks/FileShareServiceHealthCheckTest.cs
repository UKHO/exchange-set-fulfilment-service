using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Aspire.Configuration.Remote;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Health;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.HealthChecks
{
    [TestFixture]
    internal class FileShareServiceHealthCheckTest
    {
        private IHttpClientFactory _fakeHttpClientFactory;
        private IExternalServiceRegistry _fakeExternalServiceRegistry;
        private ILogger<FileShareServiceHealthCheck> _fakeLogger;
        private IExternalEndpoint _fakeExternalEndpoint;
        private MockHttpMessageHandler _mockHttpMessageHandler;
        private FileShareServiceHealthCheck _healthCheck;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
            _fakeExternalServiceRegistry = A.Fake<IExternalServiceRegistry>();
            _fakeLogger = A.Fake<ILogger<FileShareServiceHealthCheck>>();
            _fakeExternalEndpoint = A.Fake<IExternalEndpoint>();
        }

        [TearDown]
        public void TearDown()
        {
            _mockHttpMessageHandler?.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            _mockHttpMessageHandler = new MockHttpMessageHandler();
            var fakeHttpClient = new HttpClient(_mockHttpMessageHandler);

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>._)).Returns(fakeHttpClient);
            A.CallTo(() => _fakeExternalEndpoint.Uri).Returns(new Uri("https://test-service.com/"));
            A.CallTo(() => _fakeExternalServiceRegistry.GetServiceEndpoint(ProcessNames.FileShareService, "", EndpointHostSubstitution.None))
                .Returns(_fakeExternalEndpoint);

            _healthCheck = new FileShareServiceHealthCheck(_fakeHttpClientFactory, _fakeExternalServiceRegistry, _fakeLogger);
        }

        [Test]
        public async Task WhenCheckHealthAsyncIsCalledAndServiceReturnsSuccessStatusCode_ThenReturnHealthy()
        {
            _mockHttpMessageHandler.SetResponse("https://test-service.com/health", HttpStatusCode.OK);

            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
            Assert.That(result.Description, Is.EqualTo("File Share Service responded with OK"));
        }

        [Test]
        public async Task WhenCheckHealthAsyncIsCalledAndServiceReturnsNonSuccessStatusCode_ThenReturnUnhealthy()
        {
            _mockHttpMessageHandler.SetResponse("https://test-service.com/health", HttpStatusCode.InternalServerError);

            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
            Assert.That(result.Description, Is.EqualTo("File Share Service health check failed"));
            Assert.That(result.Exception, Is.Not.Null);
            Assert.That(result.Exception!.Message, Is.EqualTo("Service returned status code InternalServerError"));
        }

        [Test]
        public async Task WhenCheckHealthAsyncIsCalledAndHttpClientThrowsException_ThenReturnUnhealthy()
        {
            var exception = new HttpRequestException("Connection failed");
            _mockHttpMessageHandler.SetException("https://test-service.com/health", exception);

            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
            Assert.That(result.Description, Is.EqualTo("File Share Service health check failed"));
            Assert.That(result.Exception, Is.EqualTo(exception));
        }

        public class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly Dictionary<string, HttpResponseMessage> _responses = [];
            private readonly Dictionary<string, Exception> _exceptions = [];
            public CancellationToken CancellationToken { get; private set; }

            public void SetResponse(string uri, HttpStatusCode statusCode)
            {
                _responses[uri] = new HttpResponseMessage(statusCode)
                {
                    RequestMessage = new HttpRequestMessage(HttpMethod.Get, uri)
                };
                _exceptions.Remove(uri);
            }

            public void SetException(string uri, Exception exception)
            {
                _exceptions[uri] = exception;
                _responses.Remove(uri);
            }

            public void Reset()
            {
                _responses.Clear();
                _exceptions.Clear();
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CancellationToken = cancellationToken;
                var uri = request.RequestUri!.ToString();

                if (_exceptions.TryGetValue(uri, out var exception))
                {
                    throw exception;
                }

                if (_responses.TryGetValue(uri, out var response))
                {
                    return Task.FromResult(response);
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }
    }
}
