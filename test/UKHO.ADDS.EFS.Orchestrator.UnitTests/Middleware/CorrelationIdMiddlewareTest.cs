using FakeItEasy;
using Microsoft.AspNetCore.Http;
using UKHO.ADDS.EFS.Constants;
using UKHO.ADDS.EFS.Exceptions;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Middleware;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Middleware
{
    [TestFixture]
    public class CorrelationIdMiddlewareTest
    {
        private CorrelationIdMiddleware _middleware;
        private RequestDelegate _next;

        [SetUp]
        public void SetUp()
        {
            _next = A.Fake<RequestDelegate>();
            _middleware = new CorrelationIdMiddleware(_next);
        }

        [Test]
        public async Task WhenCorrelationIdIsMissing_ThenThrowsOrchestratorException()
        {
            // Arrange  
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/test";

            // Act  
            Func<Task> act = async () => await _middleware.InvokeAsync(context);

            // Assert  
            Assert.That(async () => await act(), Throws.TypeOf<OrchestratorException>()
                .With.Message.EqualTo("No correlation ID found in the request header"));
        }

        [Test]
        public async Task WhenPathIsExcluded_ThenDoesNotCheckForCorrelationId()
        {
            // Arrange  
            var context = new DefaultHttpContext();
            context.Request.Path = "/healthcheck";

            // Act  
            await _middleware.InvokeAsync(context);

            // Assert  
            A.CallTo(() => _next(context)).MustHaveHappenedOnceExactly();
        }
    }
}
