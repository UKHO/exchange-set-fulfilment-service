using System.Text.RegularExpressions;

namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for productIdentifier property
/// </summary>
internal static class ProductIdentifierValidator
{
    public const string VALIDATION_MESSAGE = "productIdentifier must be exactly 4 characters: start with 'S' or 's' followed by three digits, with no spaces or extra characters.";

    /// <summary>
    /// Validates the productIdentifier with an optional timeout.
    /// </summary>
    /// <param name="productIdentifier">The product identifier to validate.</param>
    /// <param name="timeoutMilliseconds">Timeout in milliseconds for regex match. Default is 5000ms.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool IsValid(string? productIdentifier, int timeoutMilliseconds = 5000)
    {
        if (productIdentifier == null)
        {
            return true;
        }
        try
        {
            return Regex.IsMatch(productIdentifier, "^[Ss]\\d{3}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(timeoutMilliseconds));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
