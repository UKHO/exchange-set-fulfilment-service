using FluentValidation;

namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for callback URI format and security requirements
/// </summary>
public class CallbackUriValidator : AbstractValidator<string?>
{

    public const string INVALID_CALLBACK_URI_MESSAGE = "Invalid callbackUri format.";
    /// <summary>
    /// Initializes a new instance of the CallbackUriValidator class
    /// </summary>
    public CallbackUriValidator()
    {
        RuleFor(callbackUri => callbackUri)
            .Must(IsValidCallbackUri)
            .WithMessage(INVALID_CALLBACK_URI_MESSAGE);
    }

    /// <summary>
    /// Validates that the callback URI is a valid HTTPS URI
    /// </summary>
    /// <param name="callbackUri">The callback URI to validate</param>
    /// <returns>True if the URI is valid HTTPS, false otherwise</returns>
    public static bool IsValidCallbackUri(string? callbackUri)
    {
        if (callbackUri == null) return true;

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
