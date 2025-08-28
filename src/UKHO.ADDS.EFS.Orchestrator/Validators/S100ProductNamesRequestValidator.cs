using FluentValidation;
using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for S100ProductNamesRequest
/// </summary>
internal class S100ProductNamesRequestValidator : AbstractValidator<S100ProductNamesRequest>
{
    public S100ProductNamesRequestValidator()
    {
        RuleFor(request => request.ProductNames)
            .Must(product =>
                product is { Count: > 0 } &&
                product.TrueForAll(data => !string.IsNullOrWhiteSpace(data)))
            .WithMessage("ProductNames cannot be null or empty.");

        RuleFor(request => request.CallbackUri)
            .Must(IsValidCallbackUri)
            .When(request => !string.IsNullOrEmpty(request.CallbackUri))
            .WithMessage("Invalid callbackUri format.");
    }

    private static bool IsValidCallbackUri(string? callbackUri)
    {
        if (string.IsNullOrEmpty(callbackUri))
            return true;

        try
        {
            Uri baseUri = new Uri(callbackUri);
            return (baseUri.Scheme == Uri.UriSchemeHttps);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
