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
            .Must(product =>
            {
                var productNameValidator = new S100ProductNamesRequestValidator();
                var result = productNameValidator.Validate(new S100ProductNamesRequest { ProductNames = [product.ProductName] });
                return result.IsValid;
            })
            .WithMessage("ProductName is invalid.")
            .Must(product => product.EditionNumber > 0)
            .WithMessage("Edition number must be a positive integer.")
            .Must(product => product.UpdateNumber >= 0)
            .WithMessage("Update number must be zero or a positive integer.");
    }
}
