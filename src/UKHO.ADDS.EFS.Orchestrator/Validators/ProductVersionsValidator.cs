using FluentValidation;

namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for S100ProductVersionsRequest
/// </summary>
internal class S100ProductVersionsRequestValidator : AbstractValidator<ProductVersionsRequest>
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
                var parts = product.Split(':');
                if (parts.Length == 3)
                {
                    var productName = parts[0];
                    if (!int.TryParse(parts[1], out var editionNumber))
                    {
                        context.AddFailure(productName, "Edition number is not a valid integer.");
                    }
                    else if (editionNumber < 1 || editionNumber > 99)
                    {
                        context.AddFailure(productName, "Edition number must be between 1 and 99.");
                    }

                    if (!int.TryParse(parts[2], out var updateNumber))
                    {
                        context.AddFailure(productName, "Update number is not a valid integer.");
                    }
                    else if (updateNumber < 0 || updateNumber > 999)
                    {
                        context.AddFailure(productName, "Update number must be between 0 and 999.");
                    }
                }
                else
                {
                    context.AddFailure(product, "Product version must be in the format ProductName:EditionNumber:UpdateNumber.");
                }
            });
    }
}

/// <summary>
/// Request model for product versions endpoint
/// </summary>
internal class ProductVersionsRequest
{
    /// <summary>
    /// List of product versions to request (format: ProductName:EditionNumber:UpdateNumber)
    /// </summary>
    public required List<string> ProductVersions { get; set; }
}
