using FluentValidation.Results;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;
using UKHO.ADDS.EFS.Orchestrator.Validators.S100;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Validator.S100
{
    [TestFixture]
    internal class S100ProductVersionsValidatorTests
    {
        private S100ProductVersionsRequestValidator _s100ProductVersionsRequestValidator;
        private const string VALID_CALLBACK_URI = "https://valid.com/callback";
        private const string INVALID_CALLBACK_URI = "http://invalid.com/callback";
        private const string VALID_PRODUCT_NAME = "101GB40079ABCDEFG";
        private const string EMPTY_PRODUCT_NAME = "";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _s100ProductVersionsRequestValidator = S100ProductVersionsRequestValidator();
        }

        [Test]
        public void WhenAllFieldsAreValid_ThenValidationSucceeds()
        {
            var productVersions = CreateProductVersions((VALID_PRODUCT_NAME, 1, 0), ("102GB40079ABCDEFG", 2, 1));
            var result = _s100ProductVersionsRequestValidator.Validate((productVersions, VALID_CALLBACK_URI));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Errors, Is.Empty);
            });
        }

        [TestCase(0, 0, "EditionNumber must be a positive integer.")]
        [TestCase(-1, 0, "EditionNumber must be a positive integer.")]
        public void WhenEditionNumberIsInvalid_ThenValidationFails(int editionNumber, int updateNumber, string expectedMessage)
        {
            var productVersions = CreateProductVersions((VALID_PRODUCT_NAME, editionNumber, updateNumber));
            var result = _s100ProductVersionsRequestValidator.Validate((productVersions, VALID_CALLBACK_URI));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == expectedMessage));
            });
        }

        [TestCase(-1, "UpdateNumber must be zero or a positive integer.")]
        public void WhenUpdateNumberIsInvalid_ThenValidationFails(int updateNumber, string expectedMessage)
        {
            var productVersions = CreateProductVersions((VALID_PRODUCT_NAME, 1, updateNumber));
            var result = _s100ProductVersionsRequestValidator.Validate((productVersions, VALID_CALLBACK_URI));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == expectedMessage));
            });
        }

        [TestCase("")]
        [TestCase("  ")]
        [TestCase(null)]
        public void WhenProductNameIsWhitespace_ThenValidationFails(string? productName)
        {
            var productVersions = CreateProductVersions((productName, 1, 0));
            var result = _s100ProductVersionsRequestValidator.Validate((productVersions, VALID_CALLBACK_URI));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e =>
                    e.ErrorMessage == "ProductName cannot be null or empty."
                    || e.ErrorMessage == $"'{productName ?? string.Empty}' is not valid: it neither starts with a 3-digit S-100 code nor has length 8 for S-57."
                ));
            });
        }

        [TestCase("https://valid.com/callback", true)]
        [TestCase(null, true)]
        [TestCase("http://invalid.com/callback", false)]
        [TestCase("not-a-uri", false)]
        public void WhenCallbackUriIsTested_ThenValidationResultIsAsExpected(string? callbackUri, bool isValid)
        {
            var productVersions = CreateProductVersions((VALID_PRODUCT_NAME, 1, 0));
            var result = _s100ProductVersionsRequestValidator.Validate((productVersions, callbackUri));
            Assert.Multiple(() =>
            {
                if (isValid)
                {
                    Assert.That(result.IsValid, Is.True);
                    Assert.That(result.Errors, Is.Empty);
                }
                else
                {
                    Assert.That(result.IsValid, Is.False);
                    Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "Please enter a valid callback URI in HTTPS format."));
                }
            });
        }

        [Test]
        public void WhenMultipleProductVersionsWithMixedValidity_ThenValidationFailsWithAllErrors()
        {
            var productVersions = CreateProductVersions(
                (VALID_PRODUCT_NAME, 1, 0),
                (EMPTY_PRODUCT_NAME, 1, 0),
                ("AnotherProduct", -1, 0),
                ("ThirdProduct", 1, -1)
            );
            var result = _s100ProductVersionsRequestValidator.Validate((productVersions, INVALID_CALLBACK_URI));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "ProductName cannot be null or empty."));
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "EditionNumber must be a positive integer."));
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "Please enter a valid callback URI in HTTPS format."));
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "'AnotherProduct' is not valid: it neither starts with a 3-digit S-100 code nor has length 8 for S-57."));
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "'ThirdProduct' is not valid: it neither starts with a 3-digit S-100 code nor has length 8 for S-57."));
            });
        }

        private S100ProductVersionsRequestValidator S100ProductVersionsRequestValidator()
        {
            return new();
        }

        private ProductVersionRequest CreateProductVersion(string? productName, int editionNumber, int updateNumber)
        {
            return new ProductVersionRequest
            {
                ProductName = productName ?? string.Empty,
                EditionNumber = editionNumber,
                UpdateNumber = updateNumber
            };
        }

        private List<ProductVersionRequest> CreateProductVersions(params (string? productName, int editionNumber, int updateNumber)[] versions)
        {
            return versions.Select(v => CreateProductVersion(v.productName, v.editionNumber, v.updateNumber)).ToList();
        }
    }
}
