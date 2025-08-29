
namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for productIdentifier property
/// </summary>
internal static class ProductIdentifierValidator
{
    public const string ValidationMessage = "productIdentifier cannot be null, empty, or contain whitespace.";

    public static bool IsValid(string? productIdentifier)
    {
        if (string.IsNullOrWhiteSpace(productIdentifier))
            return false;
        return true;
    }
}
