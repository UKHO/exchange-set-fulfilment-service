using FluentValidation;
using FluentValidation.Results;
using UKHO.ADDS.EFS.Domain.Products;
using Vogen;

namespace UKHO.ADDS.EFS.Orchestrator.Validators.S100;

/// <summary>
/// Validator for S100ProductNamesRequest
/// </summary>
internal class S100ProductNamesRequestValidator : AbstractValidator<(List<string>? productNames, string? callbackUri)>, IS100ProductNamesRequestValidator
{
    public S100ProductNamesRequestValidator()
    {
        RuleFor(request => request.productNames)
            .Custom((productNames, context) =>
            {
                if (productNames == null || productNames.Count == 0)
                {
                    context.AddFailure(new ValidationFailure("productName", $"{nameof(ProductName)} cannot be null or empty."));
                    return;
                }
                foreach (var name in productNames)
                {
                    var validation = ProductName.Validate(name!);
                    if (validation != Validation.Ok)
                    {
                        context.AddFailure(new ValidationFailure("productName", validation.ErrorMessage ?? "ProductName is not valid."));
                    }
                }
            });

        RuleFor(request => request.callbackUri)
            .Custom((callbackUri, context) =>
            {
                if (!CallbackUriValidator.IsValidCallbackUri(callbackUri))
                {
                    context.AddFailure(new ValidationFailure("callbackUri", CallbackUriValidator.INVALID_CALLBACK_URI_MESSAGE));
                }
            });
    }

    public async Task<ValidationResult> ValidateAsync((List<string>? productNames, string? callbackUri) request)
    {
        return await base.ValidateAsync(request);
    }
}
