using FluentValidation.TestHelper;
using UKHO.ADDS.EFS.Orchestrator.Validators;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Validator;

[TestFixture]
internal class CallbackUriValidatorTests
{
    [TestCase(null, true)]
    [TestCase("", true)]
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
}
