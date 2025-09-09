using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Api
{
    [TestFixture]
    internal class S100CustomExchangeSetApiRouteBuilderExtensionTests
    {
        private ILogger _logger;
        private string _correlationId;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _logger = new TestLogger();
            _correlationId = "test-correlation-id";
        }

        private static ValidationResult CreateValidationResult(bool isValid, params (string property, string message)[] errors)
        {
            if (isValid)
                return new ValidationResult();
            return new ValidationResult(errors.Select(e => new ValidationFailure(e.property, e.message)).ToList());
        }

        private static object? InvokeHandleValidationResult(ValidationResult validationResult, ILogger logger, string correlationId)
        {
            var method = typeof(S100CustomExchangeSetApiRouteBuilderExtension)
                .GetMethod("HandleValidationResult", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return method!.Invoke(null, new object[] { validationResult, logger, correlationId });
        }

        [Test]
        public void WhenValidationResultIsValid_ThenReturnsNull()
        {
            var validationResult = CreateValidationResult(true);
            var result = InvokeHandleValidationResult(validationResult, _logger, _correlationId);
            Assert.That(result, Is.Null);
        }

        [TestCase("prop1", "error1")]
        [TestCase("prop2", "error2")]
        public void WhenValidationResultIsInvalid_ThenReturnsBadRequestWithErrorResponse(string property, string message)
        {
            var validationResult = CreateValidationResult(false, (property, message));
            var result = InvokeHandleValidationResult(validationResult, _logger, _correlationId);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<BadRequest<ErrorResponseModel>>());
                var badRequest = (BadRequest<ErrorResponseModel>)result!;
                Assert.That(badRequest.Value.CorrelationId, Is.EqualTo(_correlationId));
                Assert.That(badRequest.Value.Errors.Count, Is.EqualTo(1));
                Assert.That(badRequest.Value.Errors[0].Source, Is.EqualTo(property));
                Assert.That(badRequest.Value.Errors[0].Description, Is.EqualTo(message));
            });
        }

        [Test]
        public void WhenValidationResultIsInvalidWithMultipleErrors_ThenReturnsBadRequestWithAllErrors()
        {
            var errors = new[] { ("propA", "msgA"), ("propB", "msgB") };
            var validationResult = CreateValidationResult(false, errors);
            var result = InvokeHandleValidationResult(validationResult, _logger, _correlationId);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<BadRequest<ErrorResponseModel>>());
                var badRequest = (BadRequest<ErrorResponseModel>)result!;
                Assert.That(badRequest.Value.CorrelationId, Is.EqualTo(_correlationId));
                Assert.That(badRequest.Value.Errors.Count, Is.EqualTo(errors.Length));
                Assert.That(badRequest.Value.Errors.Select(e => e.Source), Is.EquivalentTo(errors.Select(e => e.Item1)));
                Assert.That(badRequest.Value.Errors.Select(e => e.Description), Is.EquivalentTo(errors.Select(e => e.Item2)));
            });
        }

        private class TestLogger : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) => null!;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        }
    }
}
