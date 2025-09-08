using FluentValidation.TestHelper;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Validators;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Orchestrator.Validators.S100;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Validator.S100;

[TestFixture]
internal class S100ProductNamesRequestValidatorTests
{
    private S100ProductNamesRequestValidator _s100ProductNamesRequestValidator;
    private const string ProductValidationErrorMessage = "ProductName cannot be null or empty.";
    private const string CallbackUriValidationErrorMessage = CallbackUriValidator.InvalidCallbackUriMessage;

    [SetUp]
    public void SetUp()
    {
        _s100ProductNamesRequestValidator = new S100ProductNamesRequestValidator();
    }

    [Test]
    public void WhenProductNamesIsEmpty_ThenValidationFails()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = new List<string>()
        };
        var result = _s100ProductNamesRequestValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ProductNames)
            .WithErrorMessage(ProductValidationErrorMessage);
    }

    [Test]
    public void WhenProductNamesContainsInvalidProduct_ThenValidationFailsWithSpecificMessage()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = new List<string> { "", "123", "ABCDEFGH", "999GB004DEVQK" }
        };
        var result = _s100ProductNamesRequestValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ProductNames)
            .WithErrorMessage(ProductValidationErrorMessage);
        // Check for specific error messages from ProductName.Validate
        Assert.Multiple(() =>
        {
            Assert.That(result.Errors.Any(e => e.ErrorMessage.Contains("cannot be null or empty")), Is.True);
            Assert.That(result.Errors.Any(e => e.ErrorMessage.Contains("is not valid") || e.ErrorMessage.Contains("not a valid S-100 product")), Is.True);
        });
    }

    [Test]
    public void WhenProductNamesAreValid_ThenValidationPasses()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = new List<string> { "101GB004DEVQK", "102CA005N5040W00130", "104CA00_20241103T001500Z_GB3DEVK0_dcf2" }
        };
        var result = _s100ProductNamesRequestValidator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.ProductNames);
    }

    [Test]
    public void WhenCallbackUriIsNull_ThenValidationPasses()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = new List<string> { "101GB004DEVQK" },
            CallbackUri = null
        };
        var result = _s100ProductNamesRequestValidator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.CallbackUri);
    }

    [Test]
    public void WhenCallbackUriIsValidHttps_ThenValidationPasses()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = new List<string> { "101GB004DEVQK" },
            CallbackUri = "https://example.com/callback"
        };
        var result = _s100ProductNamesRequestValidator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.CallbackUri);
    }

    [Test]
    public void WhenCallbackUriIsInvalid_ThenValidationFails()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = new List<string> { "101GB004DEVQK" },
            CallbackUri = "http://example.com/callback"
        };
        var result = _s100ProductNamesRequestValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CallbackUri)
            .WithErrorMessage(CallbackUriValidationErrorMessage);
    }

    [Test]
    public void WhenCallbackUriIsMalformed_ThenValidationFails()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = new List<string> { "101GB004DEVQK" },
            CallbackUri = "not-a-valid-uri"
        };
        var result = _s100ProductNamesRequestValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CallbackUri)
            .WithErrorMessage(CallbackUriValidationErrorMessage);
    }

    [Test]
    public void WhenBothProductNamesAndCallbackUriAreValid_ThenValidationPasses()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = new List<string> { "101GB004DEVQK" },
            CallbackUri = "https://example.com/callback"
        };
        var result = _s100ProductNamesRequestValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void WhenBothProductNamesAndCallbackUriAreInvalid_ThenValidationFailsForBoth()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = new List<string>(),
            CallbackUri = "invalid-uri"
        };
        var result = _s100ProductNamesRequestValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ProductNames)
            .WithErrorMessage(ProductValidationErrorMessage);
        result.ShouldHaveValidationErrorFor(x => x.CallbackUri)
            .WithErrorMessage(CallbackUriValidationErrorMessage);
    }
}
