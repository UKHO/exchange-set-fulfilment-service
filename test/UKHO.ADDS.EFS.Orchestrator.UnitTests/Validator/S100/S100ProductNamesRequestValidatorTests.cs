using FluentValidation.Results;
using UKHO.ADDS.EFS.Orchestrator.Validators.S100;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Validator.S100;

[TestFixture]
internal class S100ProductNamesRequestValidatorTests
{
    private S100ProductNamesRequestValidator _validator;
    private const string ValidCallbackUri = "https://valid.com/callback";
    private const string InvalidCallbackUri = "http://invalid.com/callback";
    private const string NotAUri = "not-a-uri";
    private const string ValidS100ProductName = "101GB4007";
    private const string ValidS57ProductName = "ABCDEFGH";
    private const string InvalidProductName = "ABC";
    private const string EmptyProductName = "";
    private const string NullProductName = null;
    private const string ProductNameCannotBeNullOrEmptyMessage = "ProductName cannot be null or empty.";
    private const string InvalidCallbackUriFormatMessage = "Invalid callbackUri format.";
    private const string IsNotValidMessage = "is not valid";

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
        var result = await ValidateAsync(new List<string> { ValidS100ProductName, ValidS57ProductName }, ValidCallbackUri);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task WhenProductNamesIsNull_ThenValidationFails()
    {
        var result = await ValidateAsync(null, ValidCallbackUri);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == "No product Names provided."));
        });
    }

    [Test]
    public async Task WhenProductNamesIsEmpty_ThenValidationFails()
    {
        var result = await ValidateAsync(new List<string>(), ValidCallbackUri);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == ProductNameCannotBeNullOrEmptyMessage));
        });
    }

    [TestCase(EmptyProductName)]
    [TestCase(NullProductName)]
    public async Task WhenProductNameIsNullOrWhitespace_ThenValidationFails(string? productName)
    {
        var result = await ValidateAsync(new List<string> { productName }, ValidCallbackUri);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == ProductNameCannotBeNullOrEmptyMessage));
        });
    }

    [TestCase(InvalidProductName)]
    [TestCase("   ")]
    public async Task WhenProductNameIsInvalidFormat_ThenValidationFails(string productName)
    {
        var result = await ValidateAsync(new List<string> { productName }, ValidCallbackUri);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage.Contains(IsNotValidMessage)));
        });
    }

    [TestCase(ValidCallbackUri, true)]
    [TestCase(null, true)]
    [TestCase(InvalidCallbackUri, false)]
    [TestCase(NotAUri, false)]
    public async Task WhenCallbackUriIsTested_ThenValidationResultIsAsExpected(string? callbackUri, bool isValid)
    {
        var result = await ValidateAsync(new List<string> { ValidS100ProductName }, callbackUri);
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
                Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == InvalidCallbackUriFormatMessage));
            }
        });
    }

    [Test]
    public async Task WhenMultipleProductNamesWithMixedValidity_ThenValidationFailsWithAllErrors()
    {
        var result = await ValidateAsync(new List<string> { ValidS100ProductName, EmptyProductName, InvalidProductName, ValidS57ProductName }, InvalidCallbackUri);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == ProductNameCannotBeNullOrEmptyMessage));
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage.Contains(IsNotValidMessage)));
            Assert.That(result.Errors, Has.Some.Matches<ValidationFailure>(e => e.ErrorMessage == InvalidCallbackUriFormatMessage));
        });
    }
}
