using FluentValidation;
using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for S100ProductVersionsRequest
/// </summary>
internal class S100ProductVersionsRequestValidator : AbstractValidator<S100ProductVersionsRequest>
{
    public S100ProductVersionsRequestValidator()
    {
        RuleFor(request => request.ProductVersions)
            .NotNull()
            .WithMessage("ProductVersions cannot be null")
            .NotEmpty()
            .WithMessage("ProductVersions cannot be empty");

        RuleForEach(request => request.ProductVersions)
            .Must(product => product.EditionNumber > 0)
            .WithMessage("Edition number must be a positive integer.")
            .Must(product => product.UpdateNumber >= 0)
            .WithMessage("Update number must be zero or a positive integer.");

        RuleForEach(request => request.ProductVersions)
           .Must(product => !string.IsNullOrWhiteSpace(product.ProductName))
           .WithMessage("ProductNames cannot be null or empty..");

        RuleFor(request => request.CallbackUri)
            .Must(CallbackUriValidator.IsValidCallbackUri)
            .WithMessage("Invalid callbackUri format.");
    }
}
