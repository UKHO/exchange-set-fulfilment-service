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
            .NotEmpty()
            .NotNull()
            .WithMessage("ProductNames cannot be null or empty")
            .Must(productNames => productNames.Count <= 100)
            .WithMessage("Maximum of 100 product names allowed per request");

        RuleForEach(request => request.ProductNames)
            .NotNull()
            .WithMessage("Product name cannot be null.")
            .NotEmpty()
            .WithMessage("Product name cannot be empty.")
            .Must(productName => !string.IsNullOrWhiteSpace(productName))
            .WithMessage("Product name cannot be null or empty.");

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
