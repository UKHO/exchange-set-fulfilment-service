using System.Text.RegularExpressions;

namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for productIdentifier property
/// </summary>
internal static class ProductIdentifierValidator
{
    public const string VALIDATION_MESSAGE = "productIdentifier must be exactly 4 characters: start with 'S' or 's' followed by three digits, with no spaces or extra characters.";

    private static readonly Regex _productIdentifierRegex = new("^[Ss]\\d{3}$", RegexOptions.Compiled);

    public static bool IsValid(string? productIdentifier)
    {
        if (productIdentifier == null)
        {
            return true;
        }
        return _productIdentifierRegex.IsMatch(productIdentifier);
    }
}
