using FluentValidation.Results;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Validators;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Validator
{
    [TestFixture]
    public class S100UpdateSinceValidatorTests
    {
        private S100UpdateSinceValidator _s100UpdateSinceValidator;
        private const string VALID_CALLBACK_URI = "https://valid.com/callback";
        private const string VALID_PRODUCT_IDENTIFIER = "s101";

        [SetUp]
        public void SetUp()
        {
            _s100UpdateSinceValidator = new S100UpdateSinceValidator();
        }

        private static S100UpdatesSinceRequest CreateRequest(string? callbackUri, DateTime sinceDateTime, string? productIdentifier)
        {
            return new S100UpdatesSinceRequest
            {
                CallbackUri = callbackUri,
                SinceDateTime = sinceDateTime,
                ProductIdentifier = productIdentifier
            };
        }

        [Test]
        public void WhenAllFieldsAreValid_ThenValidationSucceeds()
        {
            var request = CreateRequest(VALID_CALLBACK_URI, DateTime.UtcNow.AddDays(-1), VALID_PRODUCT_IDENTIFIER);
            var result = _s100UpdateSinceValidator.Validate(request);
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void WhenCallbackUriIsInvalid_ThenValidationFails()
        {
            var request = CreateRequest("http://invalid.com/callback", DateTime.UtcNow.AddDays(-1), VALID_PRODUCT_IDENTIFIER);
            var result = _s100UpdateSinceValidator.Validate(request);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "Invalid callbackUri format."));
            });
        }

        [Test]
        public void WhenCallbackUriIsNull_ThenValidationSucceeds()
        {
            var request = CreateRequest(null, DateTime.UtcNow.AddDays(-1), VALID_PRODUCT_IDENTIFIER);
            var result = _s100UpdateSinceValidator.Validate(request);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void WhenSinceDateTimeIsDefault_ThenValidationFails()
        {
            var request = CreateRequest(VALID_CALLBACK_URI, default, VALID_PRODUCT_IDENTIFIER);
            var result = _s100UpdateSinceValidator.Validate(request);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "sinceDateTime cannot be empty."));
            });
        }

        [Test]
        public void WhenSinceDateTimeIsInFuture_ThenValidationFails()
        {
            var request = CreateRequest(VALID_CALLBACK_URI, DateTime.UtcNow.AddMinutes(1), VALID_PRODUCT_IDENTIFIER);
            var result = _s100UpdateSinceValidator.Validate(request);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "sinceDateTime cannot be a future date."));
            });
        }

        [Test]
        public void WhenSinceDateTimeIsMoreThan28DaysInPast_ThenValidationFails()
        {
            var request = CreateRequest(VALID_CALLBACK_URI, DateTime.UtcNow.AddDays(-29), VALID_PRODUCT_IDENTIFIER);
            var result = _s100UpdateSinceValidator.Validate(request);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "sinceDateTime cannot be more than 28 days in the past."));
            });
        }

        [Test]
        public void WhenSinceDateTimeIsNotInRequiredFormat_ThenValidationSucceedsBecauseFormatIsAlwaysValid()
        {
            var request = CreateRequest(VALID_CALLBACK_URI, DateTime.UtcNow.AddDays(-1), VALID_PRODUCT_IDENTIFIER);
            var result = _s100UpdateSinceValidator.Validate(request);
            Assert.That(result.IsValid, Is.True);
        }

        [TestCase("")]
        [TestCase("  ")]
        public void WhenProductIdentifierIsInvalid_ThenValidationFails(string? productIdentifier)
        {
            var request = CreateRequest(VALID_CALLBACK_URI, DateTime.UtcNow.AddDays(-1), productIdentifier);
            var result = _s100UpdateSinceValidator.Validate(request);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == ProductIdentifierValidator.ValidationMessage));
            });
        }

        [Test]
        public void WhenSinceDateTimeIsJustOver28DaysAgo_ThenValidationFails()
        {
            var request = CreateRequest(VALID_CALLBACK_URI, DateTime.UtcNow.AddDays(-28.0001), VALID_PRODUCT_IDENTIFIER);

            var result = _s100UpdateSinceValidator.Validate(request);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "sinceDateTime cannot be more than 28 days in the past."));
            });
        }

        [TestCase("S123")]
        [TestCase("s456")]
        [TestCase("S000")]
        [TestCase("s999")]
        [TestCase(null)]
        public void WhenProductIdentifierIsValidFormat_ThenValidationSucceeds(string? productIdentifier)
        {
            var request = CreateRequest(VALID_CALLBACK_URI, DateTime.UtcNow.AddDays(-1), productIdentifier);
            var result = _s100UpdateSinceValidator.Validate(request);
            Assert.That(result.IsValid, Is.True);
        }

        [TestCase("A123")]
        [TestCase("S12")]
        [TestCase("S1234")]
        [TestCase("S12A")]
        [TestCase("S 123")]
        [TestCase("s12")]
        [TestCase("1234")]
        [TestCase("s1a3")]
        public void WhenProductIdentifierIsInvalidFormat_ThenValidationFails(string productIdentifier)
        {
            var request = CreateRequest(VALID_CALLBACK_URI, DateTime.UtcNow.AddDays(-1), productIdentifier);
            var result = _s100UpdateSinceValidator.Validate(request);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == ProductIdentifierValidator.ValidationMessage));
            });
        }
    }
}
