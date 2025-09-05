using FluentValidation.TestHelper;
using UKHO.ADDS.EFS.Orchestrator.Validators;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Validator;

[TestFixture]
internal class CallbackUriValidatorTests
{
    private CallbackUriValidator _validator;
    private const string CallbackUriValidationErrorMessage = "Invalid callbackUri format.";

    [SetUp]
    public void SetUp()
    {
        _validator = new CallbackUriValidator();
    }

    #region IsValidCallbackUri Static Method Tests

    [Test]
    public void WhenCallbackUriIsNull_ThenIsValidCallbackUriReturnsTrue()
    {
        string? callbackUri = null;
        
        var result = CallbackUriValidator.IsValidCallbackUri(callbackUri);
        
        Assert.That(result, Is.True);
    }

    [Test]
    public void WhenCallbackUriIsEmpty_ThenIsValidCallbackUriReturnsTrue()
    {
        var callbackUri = string.Empty;
        
        var result = CallbackUriValidator.IsValidCallbackUri(callbackUri);
        
        Assert.That(result, Is.False);
    }

    [Test]
    public void WhenCallbackUriIsWhitespace_ThenIsValidCallbackUriReturnsTrue()
    {
        var callbackUri = "   ";
        
        var result = CallbackUriValidator.IsValidCallbackUri(callbackUri);

        Assert.That(result, Is.False);
    }

    [Test]
    public void WhenCallbackUriIsValidHttpsUri_ThenIsValidCallbackUriReturnsTrue()
    {
        var callbackUri = "https://example.com/callback";

        var result = CallbackUriValidator.IsValidCallbackUri(callbackUri);

        Assert.That(result, Is.True);
    }

    [Test]
    public void WhenCallbackUriIsHttpUri_ThenIsValidCallbackUriReturnsFalse()
    {
        var callbackUri = "http://example.com/callback";

        var result = CallbackUriValidator.IsValidCallbackUri(callbackUri);

        Assert.That(result, Is.False);
    }
    
    [Test]
    public void WhenCallbackUriIsFtpUri_ThenIsValidCallbackUriReturnsFalse()
    {
        var callbackUri = "ftp://example.com/file";

        var result = CallbackUriValidator.IsValidCallbackUri(callbackUri);

        Assert.That(result, Is.False);
    }

    [Test]
    public void WhenCallbackUriIsFileUri_ThenIsValidCallbackUriReturnsFalse()
    {
        var callbackUri = "file:///C:/temp/callback.txt";

        var result = CallbackUriValidator.IsValidCallbackUri(callbackUri);

        Assert.That(result, Is.False);
    }

    [Test]
    public void WhenCallbackUriHasInvalidFormat_ThenIsValidCallbackUriReturnsFalse()
    {
        var callbackUri = "not-a-valid-uri";

        var result = CallbackUriValidator.IsValidCallbackUri(callbackUri);

        Assert.That(result, Is.False);
    }

    [Test]
    public void WhenCallbackUriHasInvalidCharacters_ThenIsValidCallbackUriReturnsFalse()
    {
        var callbackUri = "https://exam ple.com/callback";

        var result = CallbackUriValidator.IsValidCallbackUri(callbackUri);

        Assert.That(result, Is.False);
    }

    [Test]
    public void WhenCallbackUriIsJustScheme_ThenIsValidCallbackUriReturnsFalse()
    {
        var callbackUri = "https://";

        var result = CallbackUriValidator.IsValidCallbackUri(callbackUri);

        Assert.That(result, Is.False);
    }

    [Test]
    public void WhenCallbackUriThrowsUriFormatException_ThenIsValidCallbackUriReturnsFalse()
    {
        var callbackUri = "https:///invalid-host-format";

        var result = CallbackUriValidator.IsValidCallbackUri(callbackUri);

        Assert.That(result, Is.False);
    }

    #endregion

    #region Validator Instance Tests

    [Test]
    public void WhenValidatorCreated_ThenInstanceIsNotNull()
    {
        Assert.That(_validator, Is.Not.Null);
        Assert.That(_validator, Is.InstanceOf<CallbackUriValidator>());
    }

    [Test]
    public void WhenValidatingEmptyCallbackUri_ThenValidationPasses()
    {
        var result = _validator.TestValidate(string.Empty);

        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage(CallbackUriValidationErrorMessage);
    }

    [Test]
    public void WhenValidatingWhitespaceCallbackUri_ThenValidationPasses()
    {
        var result = _validator.TestValidate("   ");

        result.ShouldHaveValidationErrorFor(x => x)
           .WithErrorMessage(CallbackUriValidationErrorMessage);
    }

    [Test]
    public void WhenValidatingValidHttpsUriWithPort_ThenValidationPasses()
    {
        var callbackUri = "https://example.com:8443/callback";

        var result = _validator.TestValidate(callbackUri);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void WhenValidatingHttpUri_ThenValidationFails()
    {
        var callbackUri = "http://example.com/callback";

        var result = _validator.TestValidate(callbackUri);

        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage(CallbackUriValidationErrorMessage);
    }

    [Test]
    public void WhenValidatingFtpUri_ThenValidationFails()
    {
        var callbackUri = "ftp://example.com/file";

        var result = _validator.TestValidate(callbackUri);

        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage(CallbackUriValidationErrorMessage);
    }

    [Test]
    public void WhenValidatingFileUri_ThenValidationFails()
    {
        var callbackUri = "file:///C:/temp/callback.txt";

        var result = _validator.TestValidate(callbackUri);

        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage(CallbackUriValidationErrorMessage);
    }

    [Test]
    public void WhenValidatingInvalidUri_ThenValidationFails()
    {
        var callbackUri = "not-a-valid-uri";

        var result = _validator.TestValidate(callbackUri);

        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage(CallbackUriValidationErrorMessage);
    }

    [Test]
    public void WhenValidatingCustomScheme_ThenValidationFails()
    {
        var callbackUri = "custom://example.com/callback";
        
        var result = _validator.TestValidate(callbackUri);
        
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage(CallbackUriValidationErrorMessage);
    }

    #endregion
}
