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
    private const string EditionNumber = nameof(Domain.Products.EditionNumber);
    private const string UpdateNumber = nameof(Domain.Products.UpdateNumber);
    private const string ProductName = nameof(Domain.Products.ProductName);

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
                        context.AddFailure(new ValidationFailure(ProductName, nameof(Domain.Products.ProductName)+" cannot be null or empty"));
                    }
                    else
                    {
                        var validation = Domain.Products.ProductName.Validate(product.ProductName!);
                        if (validation != Validation.Ok)
                        {
                            context.AddFailure(new ValidationFailure(ProductName, validation.ErrorMessage ?? nameof(Domain.Products.ProductName) + " is not valid"));
                        }
                    }

                    if (product.EditionNumber is null)
                    {
                        context.AddFailure(new ValidationFailure(EditionNumber, nameof(EditionNumber)+ " cannot be null"));
                    }
                    else
                    {
                        if((int)product.EditionNumber! <= 0)
                        {
                            context.AddFailure(new ValidationFailure(EditionNumber, nameof(product.EditionNumber)+ " must be a positive integer" ?? nameof(EditionNumber) + " is not valid"));
                        }
                    }

                    TryParseExactlyThreeDigits(product.ProductName!, out var code);

                    if (code == (int)DataStandardProductType.S101)
                    {
                        if (product.UpdateNumber == null)
                        {
                            context.AddFailure(new ValidationFailure(UpdateNumber, nameof(Domain.Products.UpdateNumber)+ " cannot be null"));
                        }
                        else
                        {
                            var updateNumberValidation = Domain.Products.UpdateNumber.Validate((int)product.UpdateNumber!);
                            if (updateNumberValidation != Validation.Ok)
                            {
                                context.AddFailure(new ValidationFailure(UpdateNumber, updateNumberValidation.ErrorMessage ?? nameof(Domain.Products.UpdateNumber) + " is not valid"));
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
                    context.AddFailure(new ValidationFailure("callbackUri", CallbackUriValidator.InvalidCallbackUriMessage));
                }
            });
    }

    public async Task<ValidationResult> ValidateAsync((IEnumerable<ProductVersionRequest>? productVersionsRequest, string? callbackUri) request)
    {
        return await base.ValidateAsync(request);
    }

    private static bool TryParseExactlyThreeDigits(ReadOnlySpan<char> span, out int value)
    {
        value = 0;

        if (span.Length < 3)
        {
            return false;
        }

        var d0 = span[0] - '0';
        var d1 = span[1] - '0';
        var d2 = span[2] - '0';

        if ((uint)d0 > 9U || (uint)d1 > 9U || (uint)d2 > 9U)
        {
            return false;
        }

        value = d0 * 100 + d1 * 10 + d2;
        return true;
    }
}
