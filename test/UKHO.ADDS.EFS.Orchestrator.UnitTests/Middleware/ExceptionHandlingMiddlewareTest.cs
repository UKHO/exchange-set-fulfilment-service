using System.Net;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Exceptions;
using UKHO.ADDS.EFS.Orchestrator.Middleware;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Middleware
{
    [TestFixture]
    public class ExceptionHandlingMiddlewareTest
    {
        private ExceptionHandlingMiddleware _middleware;
        private RequestDelegate _next;
        private ILogger<ExceptionHandlingMiddleware> _logger;
        private DefaultHttpContext _httpContext;

        [SetUp]
        public void SetUp()
        {
            _next = A.Fake<RequestDelegate>();
            _logger = A.Fake<ILogger<ExceptionHandlingMiddleware>>();
            _middleware = new ExceptionHandlingMiddleware(_next, _logger);
            _httpContext = new DefaultHttpContext();
        }

        [Test]
        public async Task WhenOrchestratorExceptionIsThrown_ThenReturnsInternalServerError()
        {
            A.CallTo(() => _next.Invoke(_httpContext))
                .Throws(new OrchestratorException("Test exception"));

            await _middleware.InvokeAsync(_httpContext);

            Assert.Multiple(() =>
            {
                Assert.That(_httpContext.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));
                Assert.That(_httpContext.Response.ContentType, Is.EqualTo("application/json; charset=utf-8"));
            });
        }

        [Test]
        public async Task WhenGenericExceptionIsThrown_ThenReturnsInternalServerError()
        {
            A.CallTo(() => _next.Invoke(_httpContext))
                .Throws(new Exception("Test exception"));

            await _middleware.InvokeAsync(_httpContext);

            Assert.Multiple(() =>
            {
                Assert.That(_httpContext.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));
                Assert.That(_httpContext.Response.ContentType, Is.EqualTo("application/json; charset=utf-8"));
            });
        }
    }
}
