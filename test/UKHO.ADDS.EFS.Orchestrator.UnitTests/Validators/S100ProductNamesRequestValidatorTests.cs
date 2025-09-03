using FluentValidation.TestHelper;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Validators;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Validators;

[TestFixture]
internal class S100ProductNamesRequestValidatorTests
{
    private S100ProductNamesRequestValidator _validator;
    private const string ProductValidationErrorMessage = "ProductNames cannot be null or empty.";
    private const string CallbackUriValidationErrorMessage = "Invalid callbackUri format.";

    [SetUp]
    public void SetUp()
    {
        _validator = new S100ProductNamesRequestValidator();
    }

    #region Constructor Tests

    [Test]
    public void WhenValidatorCreated_ThenInstanceIsNotNull()
    {
        Assert.That(_validator, Is.Not.Null);
        Assert.That(_validator, Is.InstanceOf<S100ProductNamesRequestValidator>());
    }

    #endregion

    #region ProductNames Validation Tests

    [Test]
    public void WhenProductNamesIsValid_ThenValidationPasses()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1", "Product2", "Product3"]
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.ProductNames);
    }

    [Test]
    public void WhenProductNamesHasSingleItem_ThenValidationPasses()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1"]
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.ProductNames);
    }

    [Test]
    public void WhenProductNamesIsNull_ThenValidationFails()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = null!
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ProductNames)
            .WithErrorMessage(ProductValidationErrorMessage);
    }

    [Test]
    public void WhenProductNamesIsEmpty_ThenValidationFails()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = []
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldHaveValidationErrorFor(x => x.ProductNames)
            .WithErrorMessage(ProductValidationErrorMessage);
    }

    [Test]
    public void WhenProductNamesContainsEmptyString_ThenValidationFails()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1", "", "Product3"]
        };

        var result = _validator.TestValidate(request);
        
        result.ShouldHaveValidationErrorFor(x => x.ProductNames)
            .WithErrorMessage(ProductValidationErrorMessage);
    }

    [Test]
    public void WhenProductNamesContainsNullString_ThenValidationFails()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1", null!, "Product3"]
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldHaveValidationErrorFor(x => x.ProductNames)
            .WithErrorMessage(ProductValidationErrorMessage);
    }

    [Test]
    public void WhenProductNamesContainsWhitespaceString_ThenValidationFails()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1", "   ", "Product3"]
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldHaveValidationErrorFor(x => x.ProductNames)
            .WithErrorMessage(ProductValidationErrorMessage);
    }

    [Test]
    public void WhenProductNamesContainsOnlyWhitespaceStrings_ThenValidationFails()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["  ", "\t", "\n"]
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldHaveValidationErrorFor(x => x.ProductNames)
            .WithErrorMessage(ProductValidationErrorMessage);
    }

    [Test]
    public void WhenProductNamesContainsOnlyEmptyStrings_ThenValidationFails()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["", "", ""]
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldHaveValidationErrorFor(x => x.ProductNames)
            .WithErrorMessage(ProductValidationErrorMessage);
    }

    [Test]
    public void WhenProductNamesContainsMixOfValidAndInvalidItems_ThenValidationFails()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["ValidProduct1", "", "ValidProduct2", null!, "  "]
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldHaveValidationErrorFor(x => x.ProductNames)
            .WithErrorMessage(ProductValidationErrorMessage);
    }

    [Test]
    public void WhenProductNamesContainsLongStrings_ThenValidationPasses()
    {
        var longProductName = new string('A', 1000);
        var request = new S100ProductNamesRequest
        {
            ProductNames = [longProductName, "Product2"]
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldNotHaveValidationErrorFor(x => x.ProductNames);
    }

    #endregion

    #region CallbackUri Validation Tests

    [Test]
    public void WhenCallbackUriIsNull_ThenValidationPasses()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1"],
            CallbackUri = null
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldNotHaveValidationErrorFor(x => x.CallbackUri);
    }

    [Test]
    public void WhenCallbackUriIsEmpty_ThenValidationPasses()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1"],
            CallbackUri = string.Empty
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldNotHaveValidationErrorFor(x => x.CallbackUri);
    }

    [Test]
    public void WhenCallbackUriIsValidHttpsUri_ThenValidationPasses()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1"],
            CallbackUri = "https://example.com/callback"
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldNotHaveValidationErrorFor(x => x.CallbackUri);
    }

    [Test]
    public void WhenCallbackUriIsHttpUri_ThenValidationFails()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1"],
            CallbackUri = "http://example.com/callback"
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldHaveValidationErrorFor(x => x.CallbackUri)
            .WithErrorMessage(CallbackUriValidationErrorMessage);
    }

    [Test]
    public void WhenCallbackUriIsFtpUri_ThenValidationFails()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1"],
            CallbackUri = "ftp://example.com/file"
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldHaveValidationErrorFor(x => x.CallbackUri)
            .WithErrorMessage(CallbackUriValidationErrorMessage);
    }

    [Test]
    public void WhenCallbackUriIsInvalidFormat_ThenValidationFails()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1"],
            CallbackUri = "not-a-valid-uri"
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldHaveValidationErrorFor(x => x.CallbackUri)
            .WithErrorMessage(CallbackUriValidationErrorMessage);
    }

    [Test]
    public void WhenCallbackUriHasInvalidCharacters_ThenValidationFails()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1"],
            CallbackUri = "https://exam ple.com/callback"
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldHaveValidationErrorFor(x => x.CallbackUri)
            .WithErrorMessage(CallbackUriValidationErrorMessage);
    }

    [Test]
    public void WhenCallbackUriIsFileUri_ThenValidationFails()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1"],
            CallbackUri = "file:///C:/temp/callback.txt"
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldHaveValidationErrorFor(x => x.CallbackUri)
            .WithErrorMessage(CallbackUriValidationErrorMessage);
    }

    [Test]
    public void WhenCallbackUriIsCustomScheme_ThenValidationFails()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1"],
            CallbackUri = "custom://example.com/callback"
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldHaveValidationErrorFor(x => x.CallbackUri)
            .WithErrorMessage(CallbackUriValidationErrorMessage);
    }

    [Test]
    public void WhenCallbackUriIsWhitespace_ThenValidationPasses()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1"],
            CallbackUri = "   "
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldNotHaveValidationErrorFor(x => x.CallbackUri);
    }

    #endregion

    #region Combined Validation Tests

    [Test]
    public void WhenBothProductNamesAndCallbackUriAreValid_ThenValidationPasses()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1", "Product2"],
            CallbackUri = "https://example.com/callback"
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void WhenProductNamesIsValidAndCallbackUriIsNull_ThenValidationPasses()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1", "Product2"],
            CallbackUri = null
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void WhenProductNamesIsInvalidAndCallbackUriIsValid_ThenValidationFailsForProductNames()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = [],
            CallbackUri = "https://example.com/callback"
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldHaveValidationErrorFor(x => x.ProductNames)
            .WithErrorMessage(ProductValidationErrorMessage);
        result.ShouldNotHaveValidationErrorFor(x => x.CallbackUri);
    }

    [Test]
    public void WhenProductNamesIsValidAndCallbackUriIsInvalid_ThenValidationFailsForCallbackUri()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["Product1", "Product2"],
            CallbackUri = "http://example.com/callback"
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldNotHaveValidationErrorFor(x => x.ProductNames);
        result.ShouldHaveValidationErrorFor(x => x.CallbackUri)
            .WithErrorMessage(CallbackUriValidationErrorMessage);
    }

    [Test]
    public void WhenBothProductNamesAndCallbackUriAreInvalid_ThenValidationFailsForBoth()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = [],
            CallbackUri = "invalid-uri"
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldHaveValidationErrorFor(x => x.ProductNames)
            .WithErrorMessage(ProductValidationErrorMessage);
        result.ShouldHaveValidationErrorFor(x => x.CallbackUri)
            .WithErrorMessage(CallbackUriValidationErrorMessage);
    }

    [Test]
    public void WhenRequestIsCompletelyValid_ThenNoValidationErrors()
    {
        var request = new S100ProductNamesRequest
        {
            ProductNames = ["101GB004DEVQK", "102CA005N5040W00130", "104CA00_20241103T001500Z_GB3DEVK0_dcf2"],
            CallbackUri = "https://api.ukho.gov.uk/webhook/callback?correlationId=abc123"
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
