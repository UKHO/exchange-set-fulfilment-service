namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for productIdentifier property
/// </summary>
internal static class ProductIdentifierValidator
{
    public const string ValidationMessage = "productIdentifier cannot be null, empty, or contain whitespace.";

    public static bool IsValid(string? productIdentifier)
    {
        // Return false if empty,or contains any whitespace
        if (string.IsNullOrWhiteSpace(productIdentifier) || (productIdentifier?.Any(char.IsWhiteSpace) ?? false))
            return false;
        return true;
    }
}
