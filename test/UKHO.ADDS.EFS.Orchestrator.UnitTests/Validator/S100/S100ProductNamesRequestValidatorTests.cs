using FluentValidation.Results;
using UKHO.ADDS.EFS.Orchestrator.Validators.S100;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Validator.S100;

[TestFixture]
internal class S100ProductNamesRequestValidatorTests
{
    private S100ProductNamesRequestValidator _validator;
    private const string VALID_CALLBACK_URI = "https://valid.com/callback";
    private const string INVALID_CALLBACK_URI = "http://invalid.com/callback";
    private const string NOT_A_URI = "not-a-uri";
    private const string VALID_S100_PRODUCT_NAME = "101GB4007";
    private const string VALID_S57_PRODUCT_NAME = "ABCDEFGH";
    private const string INVALID_PRODUCT_NAME = "ABC";
    private const string EMPTY_PRODUCT_NAME = "";
    private const string NULL_PRODUCT_NAME = null;
    private const string PRODUCT_NAME_CANNOT_BE_NULL_OR_EMPTY_MESSAGE = "ProductName cannot be null or empty.";
    private const string INVALID_CALLBACK_URI_FORMAT_MESSAGE = "Please enter a valid callback URI in HTTPS format.";
    private const string IS_NOT_VALID_MESSAGE = "is not valid";

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _validator = new S100ProductNamesRequestValidator();
    }

    private async Task<ValidationResult> ValidateAsync(List<string>? productNames, string? callbackUri)
    {
        return await _validator.ValidateAsync((productNames, callbackUri));
    }

    [Test]
    public async Task WhenAllFieldsAreValid_ThenValidationSucceeds()
    {
        var result = await ValidateAsync(new List<string> { VALID_S100_PRODUCT_NAME, VALID_S57_PRODUCT_NAME }, VALID_CALLBACK_URI);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task WhenProductNamesIsNull_ThenValidationFails()
    {
        var result = await ValidateAsync(null, VALID_CALLBACK_URI);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "ProductName cannot be null or empty."));
        });
    }

    [Test]
    public async Task WhenProductNamesIsEmpty_ThenValidationFails()
    {
        var result = await ValidateAsync(new List<string>(), VALID_CALLBACK_URI);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == PRODUCT_NAME_CANNOT_BE_NULL_OR_EMPTY_MESSAGE));
        });
    }

    [TestCase(EMPTY_PRODUCT_NAME)]
    [TestCase(NULL_PRODUCT_NAME)]
    public async Task WhenProductNameIsNullOrWhitespace_ThenValidationFails(string? productName)
    {
        var result = await ValidateAsync(new List<string> { productName }, VALID_CALLBACK_URI);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == PRODUCT_NAME_CANNOT_BE_NULL_OR_EMPTY_MESSAGE));
        });
    }

    [TestCase(INVALID_PRODUCT_NAME)]
    [TestCase("   ")]
    public async Task WhenProductNameIsInvalidFormat_ThenValidationFails(string productName)
    {
        var result = await ValidateAsync(new List<string> { productName }, VALID_CALLBACK_URI);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage.Contains(IS_NOT_VALID_MESSAGE)));
        });
    }

    [TestCase(VALID_CALLBACK_URI, true)]
    [TestCase(null, true)]
    [TestCase(INVALID_CALLBACK_URI, false)]
    [TestCase(NOT_A_URI, false)]
    public async Task WhenCallbackUriIsTested_ThenValidationResultIsAsExpected(string? callbackUri, bool isValid)
    {
        var result = await ValidateAsync(new List<string> { VALID_S100_PRODUCT_NAME }, callbackUri);
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
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == INVALID_CALLBACK_URI_FORMAT_MESSAGE));
            }
        });
    }

    [Test]
    public async Task WhenMultipleProductNamesWithMixedValidity_ThenValidationFailsWithAllErrors()
    {
        var result = await ValidateAsync(new List<string> { VALID_S100_PRODUCT_NAME, EMPTY_PRODUCT_NAME, INVALID_PRODUCT_NAME, VALID_S57_PRODUCT_NAME }, INVALID_CALLBACK_URI);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == PRODUCT_NAME_CANNOT_BE_NULL_OR_EMPTY_MESSAGE));
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage.Contains(IS_NOT_VALID_MESSAGE)));
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == INVALID_CALLBACK_URI_FORMAT_MESSAGE));
        });
    }
}
