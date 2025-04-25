using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using UKHO.ADDS.EFS.Constants;
using UKHO.ADDS.EFS.Exceptions;
using UKHO.ADDS.EFS.Orchestrator.Middleware;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Middleware
{
    [TestFixture]
    public class ExceptionHandlingMiddlewareTests
    {
        private RequestDelegate _fakeNextMiddleware;
        private HttpContext _fakeHttpContext;
        private ILogger<ExceptionHandlingMiddleware> _fakeLogger;
        private ExceptionHandlingMiddleware _middleware;

        [SetUp]
        public void SetUp()
        {
            _fakeNextMiddleware = A.Fake<RequestDelegate>();
            _fakeLogger = A.Fake<ILogger<ExceptionHandlingMiddleware>>();
            _fakeHttpContext = new DefaultHttpContext();

            _middleware = new ExceptionHandlingMiddleware(_fakeNextMiddleware, _fakeLogger);
        }

        [Test]
        public async Task WhenExceptionIsOfTypeException_ThenLogsErrorWithUnhandledExceptionEventId()
        {
            var memoryStream = new MemoryStream();
            _fakeHttpContext.Request.Headers.Append(ApiHeaderKeys.XCorrelationIdHeaderKey, "fakeCorrelationId");
            _fakeHttpContext.Response.Body = memoryStream;

            A.CallTo(() => _fakeNextMiddleware(_fakeHttpContext)).Throws(new Exception("fake exception"));

            await _middleware.InvokeAsync(_fakeHttpContext);

            memoryStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.That(problemDetails!.Extensions["correlationId"]!.ToString(), Is.EqualTo("fakeCorrelationId"));
            Assert.That(_fakeHttpContext.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));
            Assert.That(_fakeHttpContext.Response.ContentType, Is.EqualTo("application/json; charset=utf-8"));
            Assert.That(_fakeHttpContext.Response.Headers.ContainsKey(ApiHeaderKeys.OriginHeaderKey), Is.True);
            Assert.That(_fakeHttpContext.Response.Headers[ApiHeaderKeys.OriginHeaderKey], Is.EqualTo("Orchestrator"));

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "fake exception").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenExceptionIsOfTypeOrchestratorException_ThenLogsErrorWithOrchestratorExceptionEventId()
        {
            var memoryStream = new MemoryStream();
            _fakeHttpContext.Request.Headers.Append(ApiHeaderKeys.XCorrelationIdHeaderKey, "fakeCorrelationId");
            _fakeHttpContext.Response.Body = memoryStream;

            A.CallTo(() => _fakeNextMiddleware(_fakeHttpContext)).Throws(new OrchestratorException("fakemessage"));

            await _middleware.InvokeAsync(_fakeHttpContext);

            memoryStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.That(problemDetails!.Extensions["correlationId"]!.ToString(), Is.EqualTo("fakeCorrelationId"));
            Assert.That(_fakeHttpContext.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));
            Assert.That(_fakeHttpContext.Response.ContentType, Is.EqualTo("application/json; charset=utf-8"));
            Assert.That(_fakeHttpContext.Response.Headers.ContainsKey(ApiHeaderKeys.OriginHeaderKey), Is.True);
            Assert.That(_fakeHttpContext.Response.Headers[ApiHeaderKeys.OriginHeaderKey], Is.EqualTo("Orchestrator"));

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "fakemessage").MustHaveHappenedOnceExactly();
        }
    }
}
