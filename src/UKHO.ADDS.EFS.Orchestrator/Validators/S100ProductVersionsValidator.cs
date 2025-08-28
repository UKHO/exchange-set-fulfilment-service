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
            .Custom((product, context) =>
            {
                // Validate ProductName
                var productNameValidator = new S100ProductNamesRequestValidator();
                var productNameValidationResult = productNameValidator.Validate(new S100ProductNamesRequest { ProductNames = [product.ProductName] });
                if (!productNameValidationResult.IsValid)
                {
                    foreach (var error in productNameValidationResult.Errors)
                    {
                        context.AddFailure(product.ProductName, error.ErrorMessage);
                    }
                }

                // Validate EditionNumber
                if (product.EditionNumber <= 0)
                {
                    context.AddFailure(product.ProductName, "Edition number must be a positive integer.");
                }

                // Validate UpdateNumber
                if (product.UpdateNumber < 0)
                {
                    context.AddFailure(product.ProductName, "Update number must be zero or a positive integer.");
                }
            });
    }
}
