using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup;
using UKHO.ADDS.EFS.Builder.S100.Services;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Pipeline.Startup
{
    [TestFixture]
    public class CheckEndpointsNodeTests
    {
        private CheckEndpointsNode _node;
        private IExecutionContext<ExchangeSetPipelineContext> _context;
        private ExchangeSetPipelineContext _pipelineContext;
        private IToolClient _toolClient;
        private INodeStatusWriter _nodeStatusWriter;
        private ILoggerFactory _loggerFactory;
        private HttpMessageHandler _httpMessageHandler;

        [SetUp]
        public void SetUp()
        {
            // Mock dependencies
            _toolClient = A.Fake<IToolClient>();
            _nodeStatusWriter = A.Fake<INodeStatusWriter>();
            _loggerFactory = A.Fake<ILoggerFactory>();
            var fakeHttpHandler = new FakeDelegatingHandler
            {
                SendAsyncFunc = request =>
                {
                    // Mock specific endpoint behavior
                    if (request.RequestUri.AbsolutePath == "/health")
                    {
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                    }

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                }
            };

            var httpClient = new HttpClient(fakeHttpHandler)
            {
                BaseAddress = new Uri("http://localhost/")
            };
            

            var configuration = A.Fake<Microsoft.Extensions.Configuration.IConfiguration>();

            _pipelineContext = new ExchangeSetPipelineContext(
                configuration,
                _nodeStatusWriter,
                _toolClient,
                _loggerFactory
            )
            {
                FileShareEndpoint = "http://localhost/",
                WorkspaceAuthenticationKey = "fake-key"
            };

            _context = A.Fake<IExecutionContext<ExchangeSetPipelineContext>>();
            A.CallTo(() => _context.Subject).Returns(_pipelineContext);

            _node = new CheckEndpointsNode();
        }

        [TearDown]
        public void OneTimeTearDown()
        {
            _loggerFactory?.Dispose();
        }

        //[Test]
        //public async Task PerformExecuteAsync_WhenAllChecksPass_ReturnsSucceeded()
        //{
        //    // Arrange
        //    var pingResult = A.Fake<IResult<bool>>();
        //    var pingSuccess = true;
        //    A.CallTo(() => pingResult.IsSuccess(out pingSuccess)).Returns(true);
        //    A.CallTo(() => _toolClient.PingAsync()).Returns(Task.FromResult(pingResult));
        //    var listResult = A.Fake<IResult<string>>();
        //    string workspaceSuccess = "Workspace";
        //    A.CallTo(() => listResult.IsSuccess(out workspaceSuccess)).Returns(false);
        //    A.CallTo(() => _toolClient.ListWorkspaceAsync(A<string>._)).Returns(Task.FromResult(listResult));

        //    //// Correctly configure the behavior of the HttpMessageHandler's SendAsync method
        //    //A.CallTo(() => _httpMessageHandler.SendAsync(A<HttpRequestMessage>._, A<CancellationToken>._))
        //    //    .ReturnsLazily((HttpRequestMessage request, CancellationToken token) =>
        //    //    {
        //    //        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        //    //    });

        //    // Act
        //    var result = await _node.ExecuteAsync(_context);

        //    // Assert
        //    Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Succeeded));
        //}

        [Test]
        public async Task WhenPerformExecuteAsyncPingFails_ThenReturnsFailed()
        {
            var fakeResult = A.Fake<IResult<bool>>();
            var pingSuccess = false;
            A.CallTo(() => fakeResult.IsSuccess(out pingSuccess)).Returns(false);
            A.CallTo(() => _toolClient.PingAsync()).Returns(Task.FromResult(fakeResult));

            var result = await _node.ExecuteAsync(_context);

            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        [Test]
        public async Task WhenPerformExecuteAsyncListWorkspaceFails_ThenReturnsFailed()
        {
            // Arrange
            var pingResult = A.Fake<IResult<bool>>();
            var pingSuccess = true;
            A.CallTo(() => pingResult.IsSuccess(out pingSuccess)).Returns(true);
            A.CallTo(() => _toolClient.PingAsync()).Returns(Task.FromResult(pingResult));

            var listResult = A.Fake<IResult<string>>();
            string workspaceSuccess = null;
            A.CallTo(() => _toolClient.ListWorkspaceAsync(A<string>._)).Returns(Task.FromResult(listResult));

            // Act
            var result = await _node.ExecuteAsync(_context);

            // Assert
            Assert.That(result.Status, Is.EqualTo(NodeResultStatus.Failed));
        }

        //[Test]
        //public async Task PerformExecuteAsync_WhenCheckEndpointFails_ThrowsException()
        //{
        //    // Arrange
        //    A.CallTo(() => _toolClient.PingAsync())
        //        .Returns(Task.FromResult(Result<bool>.Success(true)));

        //    A.CallTo(() => _toolClient.ListWorkspaceAsync(A<string>._))
        //        .Returns(Task.FromResult(Result<string>.Success("Workspace")));

        //    A.CallTo(() => _httpMessageHandler.SendAsync(A<HttpRequestMessage>._, A<CancellationToken>._))
        //        .Throws<HttpRequestException>();

        //    // Act & Assert
        //    Assert.ThrowsAsync<HttpRequestException>(async () => await _node.ExecuteAsync(_context));
        //}
    }

    public class FakeDelegatingHandler : DelegatingHandler
    {
        public Func<HttpRequestMessage, Task<HttpResponseMessage>> SendAsyncFunc { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return SendAsyncFunc?.Invoke(request) ?? Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
