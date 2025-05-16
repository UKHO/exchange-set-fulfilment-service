using System.Net;
using System.Text;
using FakeItEasy;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.IIC
{
    [TestFixture]
    public class ToolClientTests
    {
        private HttpClient _httpClient;
        private ToolClient _toolClient;
        private HttpMessageHandler _httpMessageHandler;

        [SetUp]
        public void SetUp()
        {
            _httpMessageHandler = A.Fake<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandler)
            {
                BaseAddress = new Uri("http://localhost")
            };
            _toolClient = new ToolClient(_httpClient);
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, string content = "", Stream? stream = null)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = stream != null ? new StreamContent(stream) : new StringContent(content, Encoding.UTF8, "application/json")
            };
            A.CallTo(_httpMessageHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .WithReturnType<Task<HttpResponseMessage>>()
                .Returns(Task.FromResult(response));
        }

        [Test]
        public async Task WhenPingAsyncCalled_ThenSuccessStatusCodeEnsured()
        {
            SetupHttpResponse(HttpStatusCode.OK);
            Assert.That(async () => await _toolClient.PingAsync(), Throws.Nothing);
        }

        [Test]
        public void WhenPingAsyncCalled_AndNotSuccess_ThenThrows()
        {
            SetupHttpResponse(HttpStatusCode.InternalServerError);
            Assert.That(async () => await _toolClient.PingAsync(), Throws.Exception);
        }

        [Test]
        public async Task WhenAddExchangeSetAsyncCalled_AndSuccess_ThenReturnsSuccessResult()
        {
            var response = new OperationResponse { Code = 200, Type = "Success", Message = "ok" };
            SetupHttpResponse(HttpStatusCode.OK, JsonCodec.Encode(response));
            var result = await _toolClient.AddExchangeSetAsync("ex1", "auth", "corr");
            Assert.That(result.IsSuccess());
            //Assert.That(result.Value.Code, Is.EqualTo(200));
        }

        [Test]
        public async Task WhenAddExchangeSetAsyncCalled_AndFailure_ThenReturnsFailureResult()
        {
            SetupHttpResponse(HttpStatusCode.BadRequest, "{}");
            var result = await _toolClient.AddExchangeSetAsync("ex1", "auth", "corr");
            Assert.That(result.IsFailure());
        }

        [Test]
        public async Task WhenAddExchangeSetAsyncCalled_AndException_ThenReturnsFailureResult()
        {
            A.CallTo(_httpMessageHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .WithReturnType<Task<HttpResponseMessage>>()
                .Throws(new Exception("fail"));
            var result = await _toolClient.AddExchangeSetAsync("ex1", "auth", "corr");
            Assert.That(result.IsFailure());
        }

        [Test]
        public async Task WhenAddContentAsyncWithResourceLocation_AndSuccess_ThenReturnsSuccessResult()
        {
            var response = new OperationResponse { Code = 200, Type = "Success", Message = "ok" };
            SetupHttpResponse(HttpStatusCode.OK, JsonCodec.Encode(response));
            var result = await _toolClient.AddContentAsync("res", "ex1", "auth", "corr");
            Assert.That(result.IsSuccess());
        }

        [Test]
        public async Task WhenAddContentAsyncWithResourceLocation_AndFailure_ThenReturnsFailureResult()
        {
            SetupHttpResponse(HttpStatusCode.BadRequest, "{}");
            var result = await _toolClient.AddContentAsync("res", "ex1", "auth", "corr");
            Assert.That(result.IsFailure());
        }

        [Test]
        public async Task WhenAddContentAsyncWithResourceLocation_AndException_ThenReturnsFailureResult()
        {
            A.CallTo(_httpMessageHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .WithReturnType<Task<HttpResponseMessage>>()
                .Throws(new Exception("fail"));
            var result = await _toolClient.AddContentAsync("res", "ex1", "auth", "corr");
            Assert.That(result.IsFailure());
        }

        [Test]
        public async Task WhenAddContentAsyncWithoutResourceLocation_AndNoDirectories_ThenReturnsNotFoundFailure()
        {
            var dirPath = Path.Combine("/usr/local/tomcat/ROOT/spool", "spec-wise");
            Directory.CreateDirectory(dirPath);
            foreach (var d in Directory.GetDirectories(dirPath))
                Directory.Delete(d, true);

            var result = await _toolClient.AddContentAsync("ex1", "auth", "corr");
            Assert.That(result.IsFailure());
        }

        [Test]
        public async Task WhenAddContentAsyncWithoutResourceLocation_AndAllDirectoriesAdded_ThenReturnsSuccess()
        {
            var dirPath = Path.Combine("/usr/local/tomcat/ROOT/spool", "spec-wise");
            Directory.CreateDirectory(dirPath);
            var subDir = Path.Combine(dirPath, "dir1");
            Directory.CreateDirectory(subDir);

            var response = new OperationResponse { Code = 200, Type = "Success", Message = "ok" };
            SetupHttpResponse(HttpStatusCode.OK, JsonCodec.Encode(response));
            var result = await _toolClient.AddContentAsync("ex1", "auth", "corr");
            Assert.That(result.IsSuccess());
            Directory.Delete(subDir, true);
        }

        [Test]
        public async Task WhenAddContentAsyncWithoutResourceLocation_AndOneDirectoryFails_ThenReturnsFailure()
        {
            var dirPath = Path.Combine("/usr/local/tomcat/ROOT/spool", "spec-wise");
            Directory.CreateDirectory(dirPath);
            var subDir = Path.Combine(dirPath, "dir2");
            Directory.CreateDirectory(subDir);

            SetupHttpResponse(HttpStatusCode.BadRequest, "{}");
            var result = await _toolClient.AddContentAsync("ex1", "auth", "corr");
            Assert.That(result.IsFailure());
            Directory.Delete(subDir, true);
        }

        [Test]
        public async Task WhenAddContentAsyncWithoutResourceLocation_AndException_ThenReturnsFailure()
        {
            var dirPath = Path.Combine("/usr/local/tomcat/ROOT/spool", "spec-wise");
            Directory.CreateDirectory(dirPath);
            var subDir = Path.Combine(dirPath, "dir3");
            Directory.CreateDirectory(subDir);

            A.CallTo(_httpMessageHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .WithReturnType<Task<HttpResponseMessage>>()
                .Throws(new Exception("fail"));
            var result = await _toolClient.AddContentAsync("ex1", "auth", "corr");
            Assert.That(result.IsFailure());
            Directory.Delete(subDir, true);
        }

        [Test]
        public async Task WhenSignExchangeSetAsync_AndSuccess_ThenReturnsSuccess()
        {
            var response = new SigningResponse { Certificate = "cert", SigningKey = "key", Status = "ok" };
            SetupHttpResponse(HttpStatusCode.OK, JsonCodec.Encode(response));
            var result = await _toolClient.SignExchangeSetAsync("ex1", "auth", "corr");
            Assert.That(result.IsSuccess());
            //Assert.That(result.Value.Certificate, Is.EqualTo("cert"));
        }

        [Test]
        public async Task WhenSignExchangeSetAsync_AndFailure_ThenReturnsFailure()
        {
            SetupHttpResponse(HttpStatusCode.BadRequest, "{}");
            var result = await _toolClient.SignExchangeSetAsync("ex1", "auth", "corr");
            Assert.That(result.IsFailure());
        }

        [Test]
        public async Task WhenSignExchangeSetAsync_AndException_ThenReturnsFailure()
        {
            A.CallTo(_httpMessageHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .WithReturnType<Task<HttpResponseMessage>>()
                .Throws(new Exception("fail"));
            var result = await _toolClient.SignExchangeSetAsync("ex1", "auth", "corr");
            Assert.That(result.IsFailure());
        }

        [Test]
        public async Task WhenExtractExchangeSetAsync_AndSuccess_ThenReturnsSuccess()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));
            SetupHttpResponse(HttpStatusCode.OK, stream: stream);
            var result = await _toolClient.ExtractExchangeSetAsync("ex1", "auth", "corr");
            Assert.That(result.IsSuccess());
            //Assert.That(result.Value, Is.Not.Null);
        }

        [Test]
        public async Task WhenExtractExchangeSetAsync_AndFailure_ThenReturnsFailure()
        {
            SetupHttpResponse(HttpStatusCode.BadRequest, "{}");
            var result = await _toolClient.ExtractExchangeSetAsync("ex1", "auth", "corr");
            Assert.That(result.IsFailure());
        }

        [Test]
        public async Task WhenExtractExchangeSetAsync_AndException_ThenReturnsFailure()
        {
            A.CallTo(_httpMessageHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .WithReturnType<Task<HttpResponseMessage>>()
                .Throws(new Exception("fail"));
            var result = await _toolClient.ExtractExchangeSetAsync("ex1", "auth", "corr");
            Assert.That(result.IsFailure());
        }

        [Test]
        public async Task WhenListWorkspaceAsync_AndSuccess_ThenReturnsSuccess()
        {
            SetupHttpResponse(HttpStatusCode.OK, "workspace-list");
            var result = await _toolClient.ListWorkspaceAsync("auth");
            Assert.That(result.IsSuccess());
            //Assert.That(result.Value, Is.EqualTo("workspace-list"));
        }

        [Test]
        public async Task WhenListWorkspaceAsync_AndFailure_ThenReturnsFailure()
        {
            SetupHttpResponse(HttpStatusCode.BadRequest, "{}");
            var result = await _toolClient.ListWorkspaceAsync("auth");
            Assert.That(result.IsFailure());
        }

        [Test]
        public async Task WhenListWorkspaceAsync_AndException_ThenReturnsFailure()
        {
            A.CallTo(_httpMessageHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .WithReturnType<Task<HttpResponseMessage>>()
                .Throws(new Exception("fail"));
            var result = await _toolClient.ListWorkspaceAsync("auth");
            Assert.That(result.IsFailure());
        }
    }
}
