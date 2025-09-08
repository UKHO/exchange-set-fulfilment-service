using FluentValidation.TestHelper;
using UKHO.ADDS.EFS.Orchestrator.Validators;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Validator;

[TestFixture]
internal class CallbackUriValidatorTests
{
    private CallbackUriValidator _callBackUriValidator;
    private const string CallbackUriValidationErrorMessage = "Invalid callbackUri format.";

    [SetUp]
    public void SetUp()
    {
        _callBackUriValidator = CreateValidator();
    }
    
    [TestCase(null, true)]
    [TestCase("", false)]
    [TestCase("   ", false)]
    [TestCase("https://example.com/callback", true)]
    [TestCase("http://example.com/callback", false)]
    [TestCase("ftp://example.com/file", false)]
    [TestCase("file:///C:/temp/callback.txt", false)]
    [TestCase("not-a-valid-uri", false)]
    [TestCase("https://exam ple.com/callback", false)]
    [TestCase("https://", false)]
    [TestCase("https:///invalid-host-format", false)]
    public void WhenIsValidCallbackUri_ThenReturnsExpectedResult(string? callbackUri, bool expected)
    {
        var result = CallbackUriValidator.IsValidCallbackUri(callbackUri);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void WhenValidatorCreated_ThenInstanceIsNotNull()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_callBackUriValidator, Is.Not.Null);
            Assert.That(_callBackUriValidator, Is.InstanceOf<CallbackUriValidator>());
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("http://example.com/callback")]
    [TestCase("ftp://example.com/file")]
    [TestCase("file:///C:/temp/callback.txt")]
    [TestCase("not-a-valid-uri")]
    [TestCase("custom://example.com/callback")]
    public void WhenValidatingInvalidCallbackUri_ThenValidationFails(string callbackUri)
    {
        var result = _callBackUriValidator.TestValidate(callbackUri);

        Assert.Multiple(() =>
        {
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage(CallbackUriValidationErrorMessage);
        });
    }

    [TestCase("https://example.com:8443/callback")]
    [TestCase("https://example.com/callback")]
    public void WhenValidatingValidHttpsUri_ThenValidationPasses(string callbackUri)
    {
        var result = _callBackUriValidator.TestValidate(callbackUri);

        Assert.Multiple(() =>
        {
            result.ShouldNotHaveAnyValidationErrors();
        });
    }

    private CallbackUriValidator CreateValidator() => new CallbackUriValidator();
}
