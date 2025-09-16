using FluentValidation;
using FluentValidation.Results;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using Vogen;

namespace UKHO.ADDS.EFS.Orchestrator.Validators.S100
{
    /// <summary>
    /// Validator for S100ProductNamesRequest
    /// </summary>
    internal class S100ProductNamesRequestValidator : AbstractValidator<(List<string>? productNamesRequest, string? callbackUri)>, IS100ProductNamesRequestValidator
    {
        public S100ProductNamesRequestValidator()
        {
            RuleFor(request => request.productNamesRequest)
                .Custom((productNamesRequest, context) =>
                {
                    foreach (var name in productNamesRequest!)
                    {
                        var validation = ProductName.Validate(name!);
                        if (validation != Validation.Ok)
                        {
                            context.AddFailure(new ValidationFailure(nameof(ProductName), validation.ErrorMessage));
                        }
                    }
                });
            RuleFor(request => request.callbackUri)
                .Custom((callbackUri, context) =>
                {
                    if (!CallbackUriValidator.IsValidCallbackUri(callbackUri))
                    {
                        context.AddFailure(new ValidationFailure(nameof(CallbackUri), CallbackUriValidator.InvalidCallbackUriMessage));
                    }
                });
        }

        public async Task<ValidationResult> ValidateAsync((List<string>? productNamesRequest, string? callbackUri) request)
        {
            return await base.ValidateAsync(request);
        }
    }
}
