using FluentValidation.Results;
using UKHO.ADDS.EFS.Orchestrator.Validators;
using Microsoft.Extensions.Configuration;
using UKHO.ADDS.EFS.Orchestrator.Validators.S100;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Validator.S100
{
    [TestFixture]
    public class S100UpdateSinceRequestValidatorTests
    {
        private S100UpdateSinceRequestValidator _s100UpdateSinceRequestvalidator;
        private IConfiguration _configuration;
        private const string ValidCallbackUri = "https://valid.com/callback";
        private const string InvalidCallbackUri = "http://invalid.com/callback";
        private const string ValidProductIdentifier = "S122";
        private const string InvalidProductIdentifier = "X123";
        private const string InvalidDateFormat = "Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z')";
        private readonly TimeSpan _defaultMaxAge = TimeSpan.FromDays(28);

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var inMemorySettings = new Dictionary<string, string> {
                {"orchestrator:MaximumProductAge", _defaultMaxAge.ToString()}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _s100UpdateSinceRequestvalidator = new S100UpdateSinceRequestValidator(_configuration);
        }

        [Test]
        public async Task WhenRequestIsNullOrSinceDateTimeIsNull_ThenValidationFails()
        {
            var result = await ValidateAsync(null, ValidCallbackUri, ValidProductIdentifier);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "No UpdateSince date time provided"));
        }

        [Test]
        public async Task WhenCallbackUriIsInvalid_ThenValidationFails()
        {
            var result = await ValidateAsync(DateTime.UtcNow.ToString("o"), InvalidCallbackUri, ValidProductIdentifier);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == CallbackUriValidator.InvalidCallbackUriMessage));
        }

        [TestCase(null, true)]
        [TestCase("", true)]
        [TestCase("   ", false)]
        [TestCase("https://valid.com/callback", true)]
        [TestCase("http://invalid.com/callback", false)]
        [TestCase("not-a-uri", false)]
        public async Task WhenCallbackUriIsTested_ThenValidationResultIsAsExpected(string? callbackUri, bool isValid)
        {
            var result = await ValidateAsync(DateTime.UtcNow.ToString("o"), callbackUri, ValidProductIdentifier);
            if (isValid)
            {
                Assert.That(result.Errors, Has.None.Matches<ValidationFailure>(e => e.ErrorMessage == CallbackUriValidator.InvalidCallbackUriMessage));
            }
            else
            {
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == CallbackUriValidator.InvalidCallbackUriMessage));
            }
        }

        [Test]
        public async Task WhenSinceDateTimeHasNoTimeZone_ThenValidationFails()
        {
            var dt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"); // No timezone
            var result = await ValidateAsync(dt, ValidCallbackUri, ValidProductIdentifier);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == InvalidDateFormat));
        }

        [Test]
        public async Task WhenSinceDateTimeIsTooOld_ThenValidationFails()
        {
            var dt = DateTime.UtcNow.AddDays(-_defaultMaxAge.TotalDays - 1).ToString("o");
            var result = await ValidateAsync(dt, ValidCallbackUri, ValidProductIdentifier);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == $"Date time provided is more than {_defaultMaxAge.TotalDays} days in the past"));
        }

        [Test]
        public async Task WhenSinceDateTimeIsInFuture_ThenValidationFails()
        {
            var dt = DateTime.UtcNow.AddMinutes(1).ToString("o");
            var result = await ValidateAsync(dt, ValidCallbackUri, ValidProductIdentifier);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "UpdateSince date cannot be a future date"));
        }

        [Test]
        public async Task WhenProductIdentifierIsInvalid_ThenValidationFails()
        {
            var result = await ValidateAsync(DateTime.UtcNow.ToString("o"), ValidCallbackUri, InvalidProductIdentifier);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == ProductIdentifierValidator.ValidationMessage));
        }

        [TestCase("S122", true)]
        [TestCase("s122", true)]
        [TestCase("S12", false)]
        [TestCase("X123", false)]
        [TestCase("S1234", false)]
        [TestCase("S12A", false)]
        [TestCase(null, true)]
        public async Task WhenProductIdentifierIsTested_ThenValidationResultIsAsExpected(string? productIdentifier, bool isValid)
        {
            var result = await ValidateAsync(DateTime.UtcNow.ToString("o"), ValidCallbackUri, productIdentifier);
            if (isValid)
            {
                Assert.That(result.Errors, Has.None.Matches<ValidationFailure>(e => e.ErrorMessage == ProductIdentifierValidator.ValidationMessage));
            }
            else
            {
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == ProductIdentifierValidator.ValidationMessage));
            }
        }

        [Test]
        public async Task WhenAllFieldsAreValid_ThenValidationSucceeds()
        {
            var dt = DateTime.UtcNow.ToString("o");
            var result = await ValidateAsync(dt, ValidCallbackUri, ValidProductIdentifier);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public async Task WhenMultipleErrors_ThenAllAreReturned()
        {
            var dt = DateTime.UtcNow.AddDays(-_defaultMaxAge.TotalDays - 1).ToString("yyyy-MM-ddTHH:mm:ss"); // Too old, no timezone
            var result = await ValidateAsync(dt, InvalidCallbackUri, InvalidProductIdentifier);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == InvalidDateFormat));
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == $"Date time provided is more than {_defaultMaxAge.TotalDays} days in the past"));
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == CallbackUriValidator.InvalidCallbackUriMessage));
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == ProductIdentifierValidator.ValidationMessage));
        }

        [Test]
        public async Task WhenSinceDateTimeIsWhitespace_ThenValidationFails()
        {
            var result = await ValidateAsync("   ", ValidCallbackUri, ValidProductIdentifier);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "No UpdateSince date time provided"));
        }

        [Test]
        public async Task WhenSinceDateTimeIsInvalidFormat_ThenValidationFails()
        {
            var result = await ValidateAsync("not-a-date", ValidCallbackUri, ValidProductIdentifier);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == InvalidDateFormat));
        }

        private static UpdatesSinceRequest CreateRequest(string? sinceDateTime)
        {
            return new UpdatesSinceRequest { SinceDateTime = sinceDateTime };
        }

        private async Task<ValidationResult> ValidateAsync(string? sinceDateTime, string? callbackUri, string? productIdentifier)
        {
            var req = (CreateRequest(sinceDateTime), callbackUri, productIdentifier);
            return await _s100UpdateSinceRequestvalidator.ValidateAsync(req);
        }
    }
}
