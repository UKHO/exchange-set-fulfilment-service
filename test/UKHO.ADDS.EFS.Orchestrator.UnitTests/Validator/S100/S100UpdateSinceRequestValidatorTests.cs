using FluentValidation.Results;
using UKHO.ADDS.EFS.Domain.Messages;
using UKHO.ADDS.EFS.Orchestrator.Validators;
using Microsoft.Extensions.Configuration;
using FakeItEasy;
using UKHO.ADDS.EFS.Orchestrator.Validators.S100;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Validator.S100
{
    [TestFixture]
    public class S100UpdateSinceRequestValidatorTests
    {
        private S100UpdateSinceRequestValidator _validator;
        private IConfiguration _configuration;
        private const string ValidCallbackUri = "https://valid.com/callback";
        private const string InvalidCallbackUri = "http://invalid.com/callback";
        private const string ValidProductIdentifier = "S123";
        private const string InvalidProductIdentifier = "X123";
        private readonly TimeSpan _defaultMaxAge = TimeSpan.FromDays(28);

        [SetUp]
        public void SetUp()
        {
            _configuration = A.Fake<IConfiguration>();
            A.CallTo(() => _configuration["orchestrator:MaximumProductAge"]).Returns(_defaultMaxAge.ToString());
            _validator = new S100UpdateSinceRequestValidator(_configuration);
        }

        private S100UpdatesSinceRequest CreateRequest(DateTime? sinceDateTime)
        {
            return new S100UpdatesSinceRequest { SinceDateTime = sinceDateTime };
        }

        private async Task<ValidationResult> ValidateAsync(DateTime? sinceDateTime, string? callbackUri, string? productIdentifier)
        {
            var req = (CreateRequest(sinceDateTime), callbackUri, productIdentifier);
            return await _validator.ValidateAsync(req);
        }

        [Test]
        public async Task WhenRequestIsNullOrSinceDateTimeIsNull_ThenValidationFails()
        {
            var result1 = await _validator.ValidateAsync((null, ValidCallbackUri, ValidProductIdentifier));
            var result2 = await ValidateAsync(null, ValidCallbackUri, ValidProductIdentifier);
            Assert.Multiple(() =>
            {
                Assert.That(result1.IsValid, Is.False);
                Assert.That(result1.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "No since date time provided."));
                Assert.That(result2.IsValid, Is.False);
                Assert.That(result2.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "No since date time provided."));
            });
        }

        [Test]
        public async Task WhenCallbackUriIsInvalid_ThenValidationFails()
        {
            var result = await ValidateAsync(DateTime.UtcNow, InvalidCallbackUri, ValidProductIdentifier);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == CallbackUriValidator.InvalidCallbackUriMessage));
            });
        }

        [TestCase(null, true)]
        [TestCase("", false)]
        [TestCase("   ", false)]
        [TestCase("https://valid.com/callback", true)]
        [TestCase("http://invalid.com/callback", false)]
        [TestCase("not-a-uri", false)]
        public async Task WhenCallbackUriIsTested_ThenValidationResultIsAsExpected(string? callbackUri, bool isValid)
        {
            var result = await ValidateAsync(DateTime.UtcNow, callbackUri, ValidProductIdentifier);
            Assert.Multiple(() =>
            {
                if (isValid)
                {
                    Assert.That(result.Errors, Has.None.Matches<ValidationFailure>(e => e.ErrorMessage == CallbackUriValidator.InvalidCallbackUriMessage));
                }
                else
                {
                    Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == CallbackUriValidator.InvalidCallbackUriMessage));
                }
            });
        }

        [Test]
        public async Task WhenSinceDateTimeHasNoTimeZone_ThenValidationFails()
        {
            var dt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            var result = await ValidateAsync(dt, ValidCallbackUri, ValidProductIdentifier);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "No time zone provided."));
            });
        }

        [Test]
        public async Task WhenSinceDateTimeIsTooOld_ThenValidationFails()
        {
            var dt = DateTime.UtcNow.AddDays(-_defaultMaxAge.TotalDays - 1);
            var result = await ValidateAsync(dt, ValidCallbackUri, ValidProductIdentifier);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == $"Date time provided is more than {_defaultMaxAge.TotalDays} days in the past."));
            });
        }

        [Test]
        public async Task WhenSinceDateTimeIsInFuture_ThenValidationFails()
        {
            var dt = DateTime.UtcNow.AddMinutes(1);
            var result = await ValidateAsync(dt, ValidCallbackUri, ValidProductIdentifier);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "sinceDateTime cannot be a future date."));
            });
        }

        [Test]
        public async Task WhenProductIdentifierIsInvalid_ThenValidationFails()
        {
            var result = await ValidateAsync(DateTime.UtcNow, ValidCallbackUri, InvalidProductIdentifier);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == ProductIdentifierValidator.ValidationMessage));
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
            var result = await ValidateAsync(DateTime.UtcNow, ValidCallbackUri, productIdentifier);
            Assert.Multiple(() =>
            {
                if (isValid)
                {
                    Assert.That(result.Errors, Has.None.Matches<ValidationFailure>(e => e.ErrorMessage == ProductIdentifierValidator.ValidationMessage));
                }
                else
                {
                    Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == ProductIdentifierValidator.ValidationMessage));
                }
            });
        }

        [Test]
        public async Task WhenAllFieldsAreValid_ThenValidationSucceeds()
        {
            var dt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            var result = await ValidateAsync(dt, ValidCallbackUri, ValidProductIdentifier);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Errors, Is.Empty);
            });
        }

        [Test]
        public async Task WhenMultipleErrors_ThenAllAreReturned()
        {
            var dt = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-_defaultMaxAge.TotalDays - 1), DateTimeKind.Unspecified);
            var result = await ValidateAsync(dt, InvalidCallbackUri, InvalidProductIdentifier);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "No time zone provided."));
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == $"Date time provided is more than {_defaultMaxAge.TotalDays} days in the past."));
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == CallbackUriValidator.InvalidCallbackUriMessage));
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == ProductIdentifierValidator.ValidationMessage));
            });
        }
    }
}
