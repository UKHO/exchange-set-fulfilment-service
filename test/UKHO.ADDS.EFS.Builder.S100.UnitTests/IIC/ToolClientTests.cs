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

        private const string ResourceLocation = "Test Resource Location";
        private const string ExchangeSetId = "Test ExchangeSet Id";
        private const string AuthKey = "Test Auth Key";
        private const string CorrelationId = "Test Correlation Id";
        private const string ExceptionMessage = "Test ExceptionMessage";
        private const string DestinationPath= "xchg";

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

        [Test]
        public async Task WhenPingAsyncIsCalled_ThenSuccessStatusCodeEnsured()
        {
            SetupHttpResponse(HttpStatusCode.OK);
            Assert.That(async () => await _toolClient.PingAsync(), Throws.Nothing);
        }

        [Test]
        public async Task WhenPingAsyncIsCalledAndNotSuccess_ThenReturnFailure()
        {
            SetupHttpResponse(HttpStatusCode.InternalServerError);
            var result = await _toolClient.PingAsync();
            Assert.That(result.IsFailure(out var value, out var error), Is.EqualTo(true));
        }

        [Test]
        public async Task WhenAddExchangeSetAsyncIsCalledAndSuccess_ThenReturnsSuccessResult()
        {
            var response = new OperationResponse { Code = 200, Type = "Success", Message = "ok" };
            SetupHttpResponse(HttpStatusCode.OK, JsonCodec.Encode(response));
            var result = await _toolClient.AddExchangeSetAsync(ExchangeSetId, AuthKey, CorrelationId);
            Assert.That(result.IsSuccess(out var value, out var error), Is.EqualTo(true));
            Assert.That(value.Code, Is.EqualTo(200));
        }

        [Test]
        public async Task WhenAddExchangeSetAsyncIsCalledAndFailure_ThenReturnsFailureResult()
        {
            SetupHttpResponse(HttpStatusCode.BadRequest, "{}");
            var result = await _toolClient.AddExchangeSetAsync(ExchangeSetId, AuthKey, CorrelationId);
            Assert.That(result.IsFailure(out var value, out var error), Is.EqualTo(true));
            Assert.That(value.Message, Is.EqualTo("Bad request"));
        }

        [Test]
        public async Task WhenAddExchangeSetAsyncIsCalledAndException_ThenReturnsFailureResult()
        {
            A.CallTo(_httpMessageHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .WithReturnType<Task<HttpResponseMessage>>()
                .Throws(new Exception(ExceptionMessage));
            var result = await _toolClient.AddExchangeSetAsync(ExchangeSetId, AuthKey, CorrelationId);
            Assert.That(result.IsFailure(out var value, out var error), Is.EqualTo(true));
            Assert.That(value.Message, Is.EqualTo(ExceptionMessage));
        }

        [Test]
        public async Task WhenAddContentAsyncIsCalledWithResourceLocationAndSuccess_ThenReturnsSuccessResult()
        {
            var response = new OperationResponse { Code = 200, Type = "Success", Message = "ok" };
            SetupHttpResponse(HttpStatusCode.OK, JsonCodec.Encode(response));
            var result = await _toolClient.AddContentAsync(ResourceLocation, ExchangeSetId, AuthKey, CorrelationId);
            Assert.That(result.IsSuccess(out var value, out var error), Is.EqualTo(true));
            Assert.That(value.Code, Is.EqualTo(200));
        }

        [Test]
        public async Task WhenAddContentAsyncIsCalledWithResourceLocationAndFailure_ThenReturnsFailureResult()
        {
            SetupHttpResponse(HttpStatusCode.BadRequest, "{}");
            var result = await _toolClient.AddContentAsync(ResourceLocation, ExchangeSetId, AuthKey, CorrelationId);
            Assert.That(result.IsFailure(out var value, out var error));
            Assert.That(value.Message, Is.EqualTo("Bad request"));
        }

        [Test]
        public async Task WhenAddContentAsyncIsCalledWithResourceLocationAndException_ThenReturnsFailureResult()
        {
            A.CallTo(_httpMessageHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .WithReturnType<Task<HttpResponseMessage>>()
                .Throws(new Exception(ExceptionMessage));
            var result = await _toolClient.AddContentAsync(ResourceLocation, ExchangeSetId, AuthKey, CorrelationId);
            Assert.That(result.IsFailure(out var value, out var error));
            Assert.That(value.Message, Is.EqualTo(ExceptionMessage));
        }

        [Test]
        public async Task WhenAddContentAsyncIsCalledWithoutResourceLocationAndNoDirectories_ThenReturnsNotFoundFailure()
        {
            var dirPath = Path.Combine("/usr/local/tomcat/ROOT/spool", "spec-wise");
            Directory.CreateDirectory(dirPath);
            foreach (var d in Directory.GetDirectories(dirPath))
                Directory.Delete(d, true);

            var result = await _toolClient.AddContentAsync(ResourceLocation, ExchangeSetId, AuthKey, CorrelationId);
            Assert.That(result.IsFailure(out var value, out var error), Is.EqualTo(true));
        }

        [Test]
        public async Task WhenAddContentAsyncIsCalledWithoutResourceLocationAndAllDirectoriesAdded_ThenReturnsSuccess()
        {
            var dirPath = Path.Combine("/usr/local/tomcat/ROOT/spool", "spec-wise");
            Directory.CreateDirectory(dirPath);
            var subDir = Path.Combine(dirPath, "dir1");
            Directory.CreateDirectory(subDir);

            var response = new OperationResponse { Code = 200, Type = "Success", Message = "ok" };
            SetupHttpResponse(HttpStatusCode.OK, JsonCodec.Encode(response));
            var result = await _toolClient.AddContentAsync(ResourceLocation, ExchangeSetId, AuthKey, CorrelationId);
            Assert.That(result.IsSuccess(out var value, out var error), Is.EqualTo(true));
            Assert.That(value.Code, Is.EqualTo(200));
            Directory.Delete(subDir, true);
        }

        [Test]
        public async Task WhenAddContentAsyncIsCalledWithoutResourceLocationAndOneDirectoryFails_ThenReturnsFailure()
        {
            var dirPath = Path.Combine("/usr/local/tomcat/ROOT/spool", "spec-wise");
            Directory.CreateDirectory(dirPath);
            var subDir = Path.Combine(dirPath, "dir2");
            Directory.CreateDirectory(subDir);

            SetupHttpResponse(HttpStatusCode.BadRequest, "{}");
            var result = await _toolClient.AddContentAsync(ResourceLocation, ExchangeSetId, AuthKey, CorrelationId);
            Assert.That(result.IsFailure(out var value, out var error), Is.EqualTo(true));
            Assert.That(value.Message, Is.EqualTo("Bad request"));
            Directory.Delete(subDir, true);
        }

        [Test]
        public async Task WhenAddContentAsyncIsCalledWithoutResourceLocationAndException_ThenReturnsFailure()
        {
            var dirPath = Path.Combine("/usr/local/tomcat/ROOT/spool", "spec-wise");
            Directory.CreateDirectory(dirPath);
            var subDir = Path.Combine(dirPath, "dir3");
            Directory.CreateDirectory(subDir);

            A.CallTo(_httpMessageHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .WithReturnType<Task<HttpResponseMessage>>()
                .Throws(new Exception(ExceptionMessage));
            var result = await _toolClient.AddContentAsync(ResourceLocation, ExchangeSetId, AuthKey, CorrelationId);
            Assert.That(result.IsFailure(out var value, out var error), Is.EqualTo(true));
            Directory.Delete(subDir, true);
        }

        [Test]
        public async Task WhenSignExchangeSetAsyncIsCalledWithSuccess_ThenReturnsSuccess()
        {
            var response = new SigningResponse { Certificate = "cert", SigningKey = "key", Status = "ok" };
            SetupHttpResponse(HttpStatusCode.OK, JsonCodec.Encode(response));

            var result = await _toolClient.SignExchangeSetAsync(ExchangeSetId, AuthKey, CorrelationId);

            Assert.That(result.IsSuccess(out var value, out var error));
            Assert.That(value.Certificate, Is.EqualTo("cert"));
            Assert.That(value.SigningKey, Is.EqualTo("key"));
            Assert.That(value.Status, Is.EqualTo("ok"));
        }

        [Test]
        public async Task WhenSignExchangeSetAsyncIsCalledWithFailure_ThenReturnsFailure()
        {
            SetupHttpResponse(HttpStatusCode.BadRequest, "{}");
            var result = await _toolClient.SignExchangeSetAsync(ExchangeSetId, AuthKey, CorrelationId);
            Assert.That(result.IsFailure(out var value, out var error));
            Assert.That(value.Message, Is.EqualTo("Bad request"));
        }

        [Test]
        public async Task WhenSignExchangeSetAsyncIsCalledAndThrowsException_ThenReturnsFailure()
        {
            A.CallTo(_httpMessageHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .WithReturnType<Task<HttpResponseMessage>>()
                .Throws(new Exception(ExceptionMessage));

            var result = await _toolClient.SignExchangeSetAsync(ExchangeSetId, AuthKey, CorrelationId);
            Assert.That(result.IsFailure(out var value, out var error), Is.EqualTo(true));
            Assert.That(value.Message, Is.EqualTo(ExceptionMessage));
        }

        [Test]
        public async Task WhenExtractExchangeSetAsyncIsCalledWithSuccess_ThenReturnsSuccess()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));
            SetupHttpResponse(HttpStatusCode.OK, stream: stream);
            var result = await _toolClient.ExtractExchangeSetAsync(ExchangeSetId, AuthKey, CorrelationId, DestinationPath);
            Assert.That(result.IsSuccess(out var value, out var error), Is.EqualTo(true));
            Assert.That(value, Is.Not.Null);
        }

        [Test]
        public async Task WhenExtractExchangeSetAsyncIsCalledWithFailure_ThenReturnsFailure()
        {
            SetupHttpResponse(HttpStatusCode.BadRequest, "{}");
            var result = await _toolClient.ExtractExchangeSetAsync(ExchangeSetId, AuthKey, CorrelationId, DestinationPath);
            Assert.That(result.IsFailure(out var value, out var error), Is.EqualTo(true));
            Assert.That(value.Message, Is.EqualTo("Bad request"));
        }

        [Test]
        public async Task WhenExtractExchangeSetAsyncIsCalledAndThrowsException_ThenReturnsFailure()
        {
            A.CallTo(_httpMessageHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .WithReturnType<Task<HttpResponseMessage>>()
                .Throws(new Exception(ExceptionMessage));
            var result = await _toolClient.ExtractExchangeSetAsync(ExchangeSetId, AuthKey, CorrelationId, DestinationPath);
            Assert.That(result.IsFailure(out var value, out var error), Is.EqualTo(true));
            Assert.That(value.Message, Is.EqualTo(ExceptionMessage));
        }

        [Test]
        public async Task WhenListWorkspaceAsyncIsCalledAndSuccess_ThenReturnsSuccess()
        {
            SetupHttpResponse(HttpStatusCode.OK, "workspace-list");
            var result = await _toolClient.ListWorkspaceAsync(AuthKey);
            Assert.That(result.IsSuccess(out var value, out var error));
            Assert.That(value, Is.EqualTo("workspace-list"));
        }

        [Test]
        public async Task WhenListWorkspaceAsyncIsCalledWithFailure_ThenReturnsFailure()
        {
            SetupHttpResponse(HttpStatusCode.BadRequest, "{}");
            var result = await _toolClient.ListWorkspaceAsync(AuthKey);
            Assert.That(result.IsFailure(out var value, out var error), Is.EqualTo(true));
            Assert.That(value.Message, Is.EqualTo("Bad request"));
        }

        [Test]
        public async Task WhenListWorkspaceAsyncIsCalledAndThrowsException_ThenReturnsFailure()
        {
            A.CallTo(_httpMessageHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .WithReturnType<Task<HttpResponseMessage>>()
                .Throws(new Exception(ExceptionMessage));

            var result = await _toolClient.ListWorkspaceAsync(AuthKey);
            Assert.That(result.IsFailure(out var value, out var error), Is.EqualTo(true));
            Assert.That(value.Message, Is.EqualTo(ExceptionMessage));
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

        [TearDown]
        public void TearDown()
        {
            _httpClient.Dispose();
            _httpMessageHandler.Dispose();
        }
    }
}
