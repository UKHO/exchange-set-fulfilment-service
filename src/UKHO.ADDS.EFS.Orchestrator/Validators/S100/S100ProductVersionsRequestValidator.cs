using FluentValidation;
using FluentValidation.Results;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;
using Vogen;

namespace UKHO.ADDS.EFS.Orchestrator.Validators.S100;

/// <summary>
/// Validator for S100ProductVersion[] and callbackUri
/// </summary>
internal class S100ProductVersionsRequestValidator : AbstractValidator<(IEnumerable<ProductVersionRequest>? productVersionsRequest, string? callbackUri)>, IS100ProductVersionsRequestValidator
{
    private const string EDITION_NUMBER = nameof(EditionNumber);
    private const string UPDATE_NUMBER = nameof(UpdateNumber);
    private const string PRODUCT_NAME = nameof(ProductName);


    public S100ProductVersionsRequestValidator()
    {
        RuleFor(x => x.productVersionsRequest)
            .Custom((productVersions, context) =>
            {
                if (productVersions == null)
                {
                    return;
                }

                int index = 0;
                foreach (var product in productVersions)
                {
                    if (string.IsNullOrWhiteSpace(product.ProductName))
                    {
                        context.AddFailure(new ValidationFailure(PRODUCT_NAME, nameof(ProductName)+" cannot be null or empty."));
                    }
                    else
                    {
                        var validation = ProductName.Validate(product.ProductName!);
                        if (validation != Validation.Ok)
                        {
                            context.AddFailure(new ValidationFailure(PRODUCT_NAME, validation.ErrorMessage ?? nameof(ProductName) + " is not valid."));
                        }
                    }

                    if (product.EditionNumber is null)
                    {
                        context.AddFailure(new ValidationFailure(EDITION_NUMBER, nameof(EditionNumber)+ " cannot be null."));
                    }
                    else
                    {
                        var editionNumberValidation = EditionNumber.Validate((int)product.EditionNumber!);
                        if (editionNumberValidation != Validation.Ok)
                        {
                            context.AddFailure(new ValidationFailure(EDITION_NUMBER, editionNumberValidation.ErrorMessage ?? nameof(EditionNumber) + " is not valid."));
                        }
                    }

                    ProductName.TryParseExactlyThreeDigits(product.ProductName!, out var code);

                    if (code == (int)DataStandardProductType.S101)
                    {
                        if (product.UpdateNumber == null)
                        {
                            context.AddFailure(new ValidationFailure(UPDATE_NUMBER, nameof(UpdateNumber)+ " cannot be null."));
                        }
                        else
                        {
                            var updateNumberValidation = UpdateNumber.Validate((int)product.UpdateNumber!);
                            if (updateNumberValidation != Validation.Ok)
                            {
                                context.AddFailure(new ValidationFailure(UPDATE_NUMBER, updateNumberValidation.ErrorMessage ?? nameof(UpdateNumber) + " is not valid."));
                            }
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


    public async Task<ValidationResult> ValidateAsync((IEnumerable<ProductVersionRequest>? productVersionsRequest, string? callbackUri) request)
    {
        return await base.ValidateAsync(request);
    }
}
