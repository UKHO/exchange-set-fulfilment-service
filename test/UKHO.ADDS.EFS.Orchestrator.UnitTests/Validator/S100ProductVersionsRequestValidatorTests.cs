using FluentValidation.Results;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Validators;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Validator
{
    [TestFixture]
    public class S100ProductVersionsValidatorTests
    {
        private S100ProductVersionsValidator _s100ProductVersionsValidator;
        private const string VALID_CALLBACK_URI = "https://valid.com/callback";
        private const string INVALID_CALLBACK_URI = "http://invalid.com/callback";
        private const string VALID_PRODUCT_NAME = "ValidProduct";
        private const string EMPTY_PRODUCT_NAME = "";

        [SetUp]
        public void SetUp()
        {
            _s100ProductVersionsValidator = new S100ProductVersionsValidator();
        }

        private static S100ProductVersion CreateProductVersion(string productName, int editionNumber, int updateNumber)
        {
            return new S100ProductVersion
            {
                ProductName = productName,
                EditionNumber = editionNumber,
                UpdateNumber = updateNumber
            };
        }

        [Test]
        public void WhenAllFieldsAreValid_ThenValidationSucceeds()
        {
            var productVersions = new List<S100ProductVersion>
            {
                CreateProductVersion(VALID_PRODUCT_NAME, 1, 0),
                CreateProductVersion("AnotherProduct", 2, 1)
            };
            var result = _s100ProductVersionsValidator.Validate((productVersions, VALID_CALLBACK_URI));
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void WhenProductVersionsIsNull_ThenValidationFails()
        {
            var result = _s100ProductVersionsValidator.Validate((null!, VALID_CALLBACK_URI));
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "ProductVersions cannot be null"));
            });
        }

        [Test]
        public void WhenProductVersionsIsEmpty_ThenValidationFails()
        {
            var result = _s100ProductVersionsValidator.Validate((new List<S100ProductVersion>(), VALID_CALLBACK_URI));
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "ProductVersions cannot be empty"));
            });
        }

        [TestCase(0, 0, "Edition number must be a positive integer.")]
        [TestCase(-1, 0, "Edition number must be a positive integer.")]
        public void WhenEditionNumberIsInvalid_ThenValidationFails(int editionNumber, int updateNumber, string expectedMessage)
        {
            var productVersions = new List<S100ProductVersion>
            {
                CreateProductVersion(VALID_PRODUCT_NAME, editionNumber, updateNumber)
            };
            var result = _s100ProductVersionsValidator.Validate((productVersions, VALID_CALLBACK_URI));
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == expectedMessage));
            });
        }

        [TestCase(-1, "Update number must be zero or a positive integer.")]
        public void WhenUpdateNumberIsInvalid_ThenValidationFails(int updateNumber, string expectedMessage)
        {
            var productVersions = new List<S100ProductVersion>
            {
                CreateProductVersion(VALID_PRODUCT_NAME, 1, updateNumber)
            };
            var result = _s100ProductVersionsValidator.Validate((productVersions, VALID_CALLBACK_URI));
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == expectedMessage));
            });
        }

        [TestCase("")]
        [TestCase("  ")]
        [TestCase(null!)]
        public void WhenProductNameIsWhitespace_ThenValidationFails(string productName)
        {
            var productVersions = new List<S100ProductVersion>
            {
                CreateProductVersion(productName, 1, 0)
            };
            var result = _s100ProductVersionsValidator.Validate((productVersions, VALID_CALLBACK_URI));
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "ProductNames cannot be null or empty.."));
            });
        }

        [TestCase("https://valid.com/callback", true)]
        [TestCase(null, true)]
        [TestCase("http://invalid.com/callback", false)]
        [TestCase("not-a-uri", false)]
        public void WhenCallbackUriIsTested_ThenValidationResultIsAsExpected(string? callbackUri, bool isValid)
        {
            var productVersions = new List<S100ProductVersion>
            {
                CreateProductVersion(VALID_PRODUCT_NAME, 1, 0)
            };
            var result = _s100ProductVersionsValidator.Validate((productVersions, callbackUri));
            if (isValid)
            {
                Assert.That(result.IsValid, Is.True);
            }
            else
            {
                Assert.Multiple(() =>
                {
                    Assert.That(result.IsValid, Is.False);
                    Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "Invalid callbackUri format."));
                });
            }
        }

        [Test]
        public void WhenMultipleProductVersionsWithMixedValidity_ThenValidationFailsWithAllErrors()
        {
            var productVersions = new List<S100ProductVersion>
            {
                CreateProductVersion(VALID_PRODUCT_NAME, 1, 0),
                CreateProductVersion(EMPTY_PRODUCT_NAME, 1, 0),
                CreateProductVersion("AnotherProduct", -1, 0),
                CreateProductVersion("ThirdProduct", 1, -1)
            };
            var result = _s100ProductVersionsValidator.Validate((productVersions, INVALID_CALLBACK_URI));
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "ProductNames cannot be null or empty.."));
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "Edition number must be a positive integer."));
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "Update number must be zero or a positive integer."));
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "Invalid callbackUri format."));
            });
        }
    }
}
