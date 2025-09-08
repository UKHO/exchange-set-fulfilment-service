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
       .Must(productNames => productNames != null && productNames.Count > 0)
       .WithMessage($"{nameof(ProductName)} cannot be null or empty.");

        RuleForEach(request => request.productNames)
         .Custom((name, context) =>
         {
             var validation = ProductName.Validate(name!);
             if (validation != Validation.Ok)
             {
                 context.AddFailure(validation.ErrorMessage ?? "ProductName is not valid.");
             }
         });

        RuleFor(request => request.callbackUri)
            .Must(CallbackUriValidator.IsValidCallbackUri)
            .WithMessage(CallbackUriValidator.INVALID_CALLBACK_URI_MESSAGE);
    }

    public async Task<ValidationResult> ValidateAsync((List<string>? productNames, string? callbackUri) request)
    {
        if (request.productNames == null)
        {
            return new ValidationResult([ new ValidationFailure(nameof(request.productNames), "No product Names provided.") ]);
        }
        return await base.ValidateAsync(request);
    }
}
