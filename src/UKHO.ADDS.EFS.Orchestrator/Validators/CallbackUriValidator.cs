namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for callback URI format and security requirements
/// </summary>
public class CallbackUriValidator
{
    public const string INVALID_CALLBACK_URI_MESSAGE = "Please enter a valid callback URI in HTTPS format.";

    /// <summary>
    /// Validates that the callback URI is a valid HTTPS URI
    /// </summary>
    /// <param name="callbackUri">The callback URI to validate</param>
    /// <returns>True if the URI is valid HTTPS, false otherwise</returns>
    public static bool IsValidCallbackUri(string? callbackUri)
    {
        if (string.IsNullOrEmpty(callbackUri))
        {
            return true;
        }

        try
        {
            var baseUri = new Uri(callbackUri);
            if (baseUri.Scheme == Uri.UriSchemeHttps)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
