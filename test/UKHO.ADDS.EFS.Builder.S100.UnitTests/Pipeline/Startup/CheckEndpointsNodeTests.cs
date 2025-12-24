using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Startup
{
    [TestFixture]
    public class CheckEndpointsNodeTests
    {
        private IHttpClientFactory _fakeHttpClientFactory;
        private MockHttpMessageHandler _mockHttpMessageHandler;
        private CheckEndpointsNode _checkEndpointsNode;
        private IExecutionContext<S100ExchangeSetPipelineContext> _context;
        private IToolClient _toolClient;
        private ILoggerFactory _loggerFactory;

        [SetUp]
        public void SetUp()
        {
            _toolClient = A.Fake<IToolClient>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
            _mockHttpMessageHandler = new MockHttpMessageHandler();

            var fakeHttpClient = new HttpClient(_mockHttpMessageHandler);
            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>._)).Returns(fakeHttpClient);

            _context = A.Fake<IExecutionContext<S100ExchangeSetPipelineContext>>();

            var configuration = A.Fake<IConfiguration>();
            var pipelineContext = new S100ExchangeSetPipelineContext(
                configuration,
                _toolClient,
                null!,
                null!,
                _loggerFactory
            )
            {
                FileShareEndpoint = "https://test-endpoint/",
                FileShareHealthEndpoint = "https://test-endpoint/health",
                WorkspaceAuthenticationKey = "Test123"
            };

            A.CallTo(() => _context.Subject).Returns(pipelineContext);

            _checkEndpointsNode = new CheckEndpointsNode(_fakeHttpClientFactory);
        }

        [Test]
        public async Task WhenPerformExecuteAsyncAllEndpointsAreSuccessful_ThenReturnSucceeded()
        {
            var pingResult = A.Fake<IResult<bool>>();
            var pingSuccess = true;
            A.CallTo(() => pingResult.IsSuccess(out pingSuccess)).Returns(true);
            A.CallTo(() => _toolClient.PingAsync()).Returns(Task.FromResult(pingResult));

            var listResult = A.Fake<IResult<string>>();
            var workspaceSuccess = "Workspace";
            A.CallTo(() => listResult.IsSuccess(out workspaceSuccess)).Returns(true);
            A.CallTo(() => _toolClient.ListWorkspaceAsync(A<string>._)).Returns(Task.FromResult(listResult));

            _mockHttpMessageHandler.SetResponse("https://test-endpoint/health", HttpStatusCode.OK);

            var result = await _checkEndpointsNode.ExecuteAsync(_context);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncPingFails_ThenReturnFailed()
        {
            var pingResult = A.Fake<IResult<bool>>();
            var pingSuccess = false;
            A.CallTo(() => pingResult.IsSuccess(out pingSuccess)).Returns(false);
            A.CallTo(() => _toolClient.PingAsync()).Returns(Task.FromResult(pingResult));

            var result = await _checkEndpointsNode.ExecuteAsync(_context);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncListWorkspaceFails_ThenReturnFailed()
        {
            var listResult = A.Fake<IResult<string>>();
            var workspaceSuccess = "Workspace";
            A.CallTo(() => listResult.IsSuccess(out workspaceSuccess)).Returns(false);
            A.CallTo(() => _toolClient.ListWorkspaceAsync(A<string>._)).Returns(Task.FromResult(listResult));

            var result = await _checkEndpointsNode.ExecuteAsync(_context);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }
        [TearDown]
        public void TearDown()
        {
            _mockHttpMessageHandler?.Dispose();
            _loggerFactory?.Dispose();

        }
    }

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, HttpResponseMessage> _responses = [];

        public void SetResponse(string uri, HttpStatusCode statusCode)
        {
            _responses[uri] = new HttpResponseMessage(statusCode) { RequestMessage = new HttpRequestMessage(HttpMethod.Get, uri) };
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responses.TryGetValue(request.RequestUri!.ToString(), out var response))
            {
                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
