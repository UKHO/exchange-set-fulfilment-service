using FluentValidation;
using UKHO.ADDS.EFS.Domain.Messages;
using UKHO.ADDS.EFS.Domain.Products;
using Vogen;

namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for S100ProductVersion[] and callbackUri
/// </summary>
internal class S100ProductVersionsValidator : AbstractValidator<(IEnumerable<S100ProductVersion> productVersions, string? callbackUri)>
{
    public S100ProductVersionsValidator()
    {
        RuleFor(x => x.productVersions)
            .NotNull()
            .WithMessage("ProductVersions cannot be null")
            .NotEmpty()
            .WithMessage("ProductVersions cannot be empty");

        RuleForEach(x => x.productVersions)
            .Must(product => product.EditionNumber > 0)
            .WithMessage("Edition number must be a positive integer.")
            .Must(product => product.UpdateNumber >= 0)
            .WithMessage("Update number must be zero or a positive integer.");

        RuleForEach(x => x.productVersions)
            .Custom((product, context) =>
            {
                var validation = ProductName.Validate(product.ProductName);
                if (validation != Validation.Ok)
                {
                    context.AddFailure(validation.ErrorMessage ?? "ProductName is not valid.");
                }
            });

        RuleFor(x => x.callbackUri)
            .Must(uri => CallbackUriValidator.IsValidCallbackUri(uri))
            .WithMessage(CallbackUriValidator.InvalidCallbackUriMessage);
    }
}
