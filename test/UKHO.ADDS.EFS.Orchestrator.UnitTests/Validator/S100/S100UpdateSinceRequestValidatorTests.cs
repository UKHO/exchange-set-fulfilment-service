using FluentValidation.Results;
using UKHO.ADDS.EFS.Domain.Messages;
using UKHO.ADDS.EFS.Orchestrator.Validators;
using Microsoft.Extensions.Configuration;
using UKHO.ADDS.EFS.Orchestrator.Validators.S100;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Validator.S100
{
    [TestFixture]
    public class S100UpdateSinceRequestValidatorTests
    {
        private S100UpdateSinceRequestValidator _s100UpdateSinceRequestvalidator;
        private IConfiguration _configuration;
        private const string VALID_CALLBACK_URI = "https://valid.com/callback";
        private const string INVALID_CALLBACK_URI = "http://invalid.com/callback";
        private const string VALID_PRODUCT_IDENTIFIER = "S123";
        private const string INVALID_PRODUCT_IDENTIFIER = "X123";
        private const string INVALID_DATE_FORMAT = "Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z').";
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
            var result = await ValidateAsync(null, VALID_CALLBACK_URI, VALID_PRODUCT_IDENTIFIER);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "No since date time provided."));
            });
        }

        [Test]
        public async Task WhenCallbackUriIsInvalid_ThenValidationFails()
        {
            var result = await ValidateAsync(DateTime.UtcNow.ToString("o"), INVALID_CALLBACK_URI, VALID_PRODUCT_IDENTIFIER);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == CallbackUriValidator.INVALID_CALLBACK_URI_MESSAGE));
            });
        }

        [TestCase(null, true)]
        [TestCase("", true)]
        [TestCase("   ", false)]
        [TestCase("https://valid.com/callback", true)]
        [TestCase("http://invalid.com/callback", false)]
        [TestCase("not-a-uri", false)]
        public async Task WhenCallbackUriIsTested_ThenValidationResultIsAsExpected(string? callbackUri, bool isValid)
        {
            var result = await ValidateAsync(DateTime.UtcNow.ToString("o"), callbackUri, VALID_PRODUCT_IDENTIFIER);
            Assert.Multiple(() =>
            {
                if (isValid)
                {
                    Assert.That(result.Errors, Has.None.Matches<ValidationFailure>(e => e.ErrorMessage == CallbackUriValidator.INVALID_CALLBACK_URI_MESSAGE));
                }
                else
                {
                    Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == CallbackUriValidator.INVALID_CALLBACK_URI_MESSAGE));
                }
            });
        }

        [Test]
        public async Task WhenSinceDateTimeHasNoTimeZone_ThenValidationFails()
        {
            var dt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"); // No timezone
            var result = await ValidateAsync(dt, VALID_CALLBACK_URI, VALID_PRODUCT_IDENTIFIER);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == INVALID_DATE_FORMAT));
            });
        }

        [Test]
        public async Task WhenSinceDateTimeIsTooOld_ThenValidationFails()
        {
            var dt = DateTime.UtcNow.AddDays(-_defaultMaxAge.TotalDays - 1).ToString("o");
            var result = await ValidateAsync(dt, VALID_CALLBACK_URI, VALID_PRODUCT_IDENTIFIER);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == $"Date time provided is more than {_defaultMaxAge.TotalDays} days in the past."));
            });
        }

        [Test]
        public async Task WhenSinceDateTimeIsInFuture_ThenValidationFails()
        {
            var dt = DateTime.UtcNow.AddMinutes(1).ToString("o");
            var result = await ValidateAsync(dt, VALID_CALLBACK_URI, VALID_PRODUCT_IDENTIFIER);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "sinceDateTime cannot be a future date."));
            });
        }

        [Test]
        public async Task WhenProductIdentifierIsInvalid_ThenValidationFails()
        {
            var result = await ValidateAsync(DateTime.UtcNow.ToString("o"), VALID_CALLBACK_URI, INVALID_PRODUCT_IDENTIFIER);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == ProductIdentifierValidator.VALIDATION_MESSAGE));
            });
        }

        [TestCase("S123", true)]
        [TestCase("s123", true)]
        [TestCase("S12", false)]
        [TestCase("X123", false)]
        [TestCase("S1234", false)]
        [TestCase("S12A", false)]
        [TestCase(null, true)]
        public async Task WhenProductIdentifierIsTested_ThenValidationResultIsAsExpected(string? productIdentifier, bool isValid)
        {
            var result = await ValidateAsync(DateTime.UtcNow.ToString("o"), VALID_CALLBACK_URI, productIdentifier);
            Assert.Multiple(() =>
            {
                if (isValid)
                {
                    Assert.That(result.Errors, Has.None.Matches<ValidationFailure>(e => e.ErrorMessage == ProductIdentifierValidator.VALIDATION_MESSAGE));
                }
                else
                {
                    Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == ProductIdentifierValidator.VALIDATION_MESSAGE));
                }
            });
        }

        [Test]
        public async Task WhenAllFieldsAreValid_ThenValidationSucceeds()
        {
            var dt = DateTime.UtcNow.ToString("o");
            var result = await ValidateAsync(dt, VALID_CALLBACK_URI, VALID_PRODUCT_IDENTIFIER);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Errors, Is.Empty);
            });
        }

        [Test]
        public async Task WhenMultipleErrors_ThenAllAreReturned()
        {
            var dt = DateTime.UtcNow.AddDays(-_defaultMaxAge.TotalDays - 1).ToString("yyyy-MM-ddTHH:mm:ss"); // Too old, no timezone
            var result = await ValidateAsync(dt, INVALID_CALLBACK_URI, INVALID_PRODUCT_IDENTIFIER);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == INVALID_DATE_FORMAT));
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == $"Date time provided is more than {_defaultMaxAge.TotalDays} days in the past."));
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == CallbackUriValidator.INVALID_CALLBACK_URI_MESSAGE));
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == ProductIdentifierValidator.VALIDATION_MESSAGE));
            });
        }

        [Test]
        public async Task WhenSinceDateTimeIsWhitespace_ThenValidationFails()
        {
            var result = await ValidateAsync("   ", VALID_CALLBACK_URI, VALID_PRODUCT_IDENTIFIER);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "No since date time provided."));
            });
        }

        [Test]
        public async Task WhenSinceDateTimeIsInvalidFormat_ThenValidationFails()
        {
            var result = await ValidateAsync("not-a-date", VALID_CALLBACK_URI, VALID_PRODUCT_IDENTIFIER);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == INVALID_DATE_FORMAT));
            });
        }

        private static S100UpdatesSinceRequest CreateRequest(string? sinceDateTime)
        {
            return new S100UpdatesSinceRequest { SinceDateTime = sinceDateTime };
        }

        private async Task<ValidationResult> ValidateAsync(string? sinceDateTime, string? callbackUri, string? productIdentifier)
        {
            var req = (CreateRequest(sinceDateTime), callbackUri, productIdentifier);
            return await _s100UpdateSinceRequestvalidator.ValidateAsync(req);
        }
    }
}
