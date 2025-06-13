using System.Net;
using FakeItEasy;
using UKHO.ADDS.EFS.Builder.S100.Services;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Services
{
    [TestFixture]
    public class NodeStatusWriterTests
    {
        private IHttpClientFactory _fakeHttpClientFactory;
        private HttpClient _fakeHttpClient;
        private NodeStatusWriter _nodeStatusWriter;
        private FakeHttpMessageHandler _fakeHttpMessageHandler;
        private const string BuildServiceEndpoint = "http://buildservice.local";

        [SetUp]
        public void Setup()
        {
            _fakeHttpMessageHandler = new FakeHttpMessageHandler();
            _fakeHttpClient = new HttpClient(_fakeHttpMessageHandler) { BaseAddress = new Uri("http://localhost") };
            _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>._)).Returns(_fakeHttpClient);

            _nodeStatusWriter = new NodeStatusWriter(_fakeHttpClientFactory);
        }

        [Test]
        public async Task WhenWriteNodeStatusTelemetryIsCalled_ThenItShouldSendHttpPostRequest()
        {
            var nodeStatus = new ExchangeSetBuilderNodeStatus { Status = NodeResultStatus.Succeeded };
            var expectedUri = new Uri(new Uri(BuildServiceEndpoint), "/status");

            _fakeHttpMessageHandler.SetResponse(expectedUri, HttpMethod.Post, new HttpResponseMessage(HttpStatusCode.OK));

            await _nodeStatusWriter.WriteNodeStatusTelemetry(nodeStatus, BuildServiceEndpoint);

            Assert.That(_fakeHttpMessageHandler.WasRequestMade(expectedUri, HttpMethod.Post), Is.True);
        }

        [Test]
        public async Task WhenWriteDebugExchangeSetJobIsCalled_ThenItShouldSendPostRequest()
        {
            var exchangeSetJob = new ExchangeSetJob { Id = Guid.NewGuid().ToString() };
            var expectedUri = new Uri(new Uri(BuildServiceEndpoint), $"/jobs/debug/{exchangeSetJob.Id}");

            _fakeHttpMessageHandler.SetResponse(expectedUri, HttpMethod.Post, new HttpResponseMessage(HttpStatusCode.OK));

            await _nodeStatusWriter.WriteDebugExchangeSetJob(exchangeSetJob, BuildServiceEndpoint);

            Assert.That(_fakeHttpMessageHandler.WasRequestMade(expectedUri, HttpMethod.Post), Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            _fakeHttpClient?.Dispose();
            _fakeHttpMessageHandler?.Dispose();

        }
    }

    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<(Uri, HttpMethod), HttpResponseMessage> _responses = new();
        private readonly HashSet<(Uri, HttpMethod)> _requestsMade = [];

        public void SetResponse(Uri uri, HttpMethod method, HttpResponseMessage response)
        {
            _responses[(uri, method)] = response;
        }

        public bool WasRequestMade(Uri uri, HttpMethod method)
        {
            return _requestsMade.Contains((uri, method));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requestsMade.Add((request.RequestUri, request.Method));

            if (_responses.TryGetValue((request.RequestUri, request.Method), out var response))
            {
                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
