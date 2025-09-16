using FluentValidation;
using FluentValidation.Results;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;
using Vogen;

namespace UKHO.ADDS.EFS.Orchestrator.Validators.S100
{
    /// <summary>
    /// Validator for S100ProductVersion[] and callbackUri
    /// </summary>
    internal class S100ProductVersionsRequestValidator : AbstractValidator<(IEnumerable<ProductVersionRequest>? productVersionsRequest, string? callbackUri)>, IS100ProductVersionsRequestValidator
    {
        private const string EditionNumberConst = nameof(EditionNumber);
        private const string UpdateNumberConst = nameof(UpdateNumber);
        private const string ProductNameConst = nameof(ProductName);

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
                        var productNameValidation = ProductName.Validate(product.ProductName!);
                        if (productNameValidation != Validation.Ok)
                        {
                            context.AddFailure(new ValidationFailure(ProductNameConst, productNameValidation.ErrorMessage));
                        }

                        //if EditionNumber is 0 or null set to -1 so that model validation is triggered. (Edition cannot be 0)
                        var editionValidation = EditionNumber.Validate((product.EditionNumber == null || product.EditionNumber == 0) ? -1 : product.EditionNumber.Value);
                        if (editionValidation != Validation.Ok)
                        {
                            context.AddFailure(new ValidationFailure(EditionNumberConst, editionValidation.ErrorMessage));
                        }

                        TryParseExactlyThreeDigits(product.ProductName!, out var code);

                        if (code == (int)DataStandardProductType.S101)
                        {
                            var updateNumberValidation = UpdateNumber.Validate(product.UpdateNumber ?? -1);
                            if (updateNumberValidation != Validation.Ok)
                            {
                                context.AddFailure(new ValidationFailure(UpdateNumberConst, updateNumberValidation.ErrorMessage));
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
}
