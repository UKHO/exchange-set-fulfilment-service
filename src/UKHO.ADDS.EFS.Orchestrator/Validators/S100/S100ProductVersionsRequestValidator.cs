using FluentValidation;
using FluentValidation.Results;
using UKHO.ADDS.EFS.Domain.Messages;
using UKHO.ADDS.EFS.Domain.Products;
using Vogen;

namespace UKHO.ADDS.EFS.Orchestrator.Validators.S100;

/// <summary>
/// Validator for S100ProductVersion[] and callbackUri
/// </summary>
internal class S100ProductVersionsRequestValidator : AbstractValidator<(IEnumerable<S100ProductVersion>? productVersions, string? callbackUri)>, IS100ProductVersionsRequestValidator
{
    public S100ProductVersionsRequestValidator()
    {
        RuleFor(x => x.productVersions)
            .Custom((productVersions, context) =>
            {
                if (productVersions == null || !productVersions.Any())
                {
                    context.AddFailure(new ValidationFailure("productVersions", "ProductVersions cannot be empty."));
                    return;
                }
            });

        RuleFor(x => x.productVersions)
            .Custom((productVersions, context) =>
            {
                if (productVersions == null)
                {
                    return;
                }

                int index = 0;
                foreach (var product in productVersions)
                {
                    if (product.EditionNumber is null || product.EditionNumber <= 0)
                    {
                        context.AddFailure(new ValidationFailure("editionNumber", "Edition number must be a positive integer."));
                    }
                    if (product.UpdateNumber is null || product.UpdateNumber < 0)
                    {
                        context.AddFailure(new ValidationFailure("updateNumber", "Update number must be zero or a positive integer."));
                    }
                    if (string.IsNullOrWhiteSpace(product.ProductName))
                    {
                        context.AddFailure(new ValidationFailure("productName", "ProductName cannot be null or empty."));
                    }
                    else
                    {
                        var validation = ProductName.Validate(product.ProductName!);
                        if (validation != Validation.Ok)
                        {
                            context.AddFailure(new ValidationFailure("productName", validation.ErrorMessage ?? nameof(ProductName) + " is not valid."));
                        }
                    }
                    index++;
                }
            });

        RuleFor(x => x.callbackUri)
            .Custom((callbackUri, context) =>
            {
                if (!CallbackUriValidator.IsValidCallbackUri(callbackUri))
                {
                    context.AddFailure(new ValidationFailure("callbackUri", CallbackUriValidator.INVALID_CALLBACK_URI_MESSAGE));
                }
            });
    }


    public async Task<ValidationResult> ValidateAsync((IEnumerable<S100ProductVersion>? productVersions, string? callbackUri) request)
    {
        if (request.productVersions == null)
        {
            return new ValidationResult([ new ValidationFailure(nameof(request.productVersions), "No Product Versions provided.")]);
        }

        return await base.ValidateAsync(request);
    }
}
