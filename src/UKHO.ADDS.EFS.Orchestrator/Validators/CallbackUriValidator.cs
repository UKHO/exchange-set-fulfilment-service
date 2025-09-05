using FluentValidation;

namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for callback URI format and security requirements
/// </summary>
public class CallbackUriValidator : AbstractValidator<string?>
{

    public const string InvalidCallbackUriMessage = "Invalid callbackUri format.";
    /// <summary>
    /// Initializes a new instance of the CallbackUriValidator class
    /// </summary>
    public CallbackUriValidator()
    {
        RuleFor(callbackUri => callbackUri)
            .Must(IsValidCallbackUri)
            .When(callbackUri => !string.IsNullOrWhiteSpace(callbackUri))
            .WithMessage(InvalidCallbackUriMessage);
    }

    /// <summary>
    /// Validates that the callback URI is a valid HTTPS URI
    /// </summary>
    /// <param name="callbackUri">The callback URI to validate</param>
    /// <returns>True if the URI is valid HTTPS, false otherwise</returns>
    public static bool IsValidCallbackUri(string? callbackUri)
    {
        if (string.IsNullOrWhiteSpace(callbackUri))
            return true;

        try
        {
            var baseUri = new Uri(callbackUri);
            return (baseUri.Scheme == Uri.UriSchemeHttps);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
