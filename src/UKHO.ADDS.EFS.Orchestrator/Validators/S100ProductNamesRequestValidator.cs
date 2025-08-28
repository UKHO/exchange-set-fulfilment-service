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
            .Must(CallbackUriValidator.IsValidCallbackUri)
            .When(request => !string.IsNullOrEmpty(request.CallbackUri))
            .WithMessage("Invalid callbackUri format.");
    }
}
