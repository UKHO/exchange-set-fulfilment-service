using FluentValidation;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Domain.Products;
using Vogen;

namespace UKHO.ADDS.EFS.Orchestrator.Validators.S100;

/// <summary>
/// Validator for S100ProductNamesRequest
/// </summary>
internal class S100ProductNamesRequestValidator : AbstractValidator<S100ProductNamesRequest>
{
    public S100ProductNamesRequestValidator()
    {
        RuleFor(request => request.ProductNames)
            .NotNull()
            .Must(productNames => productNames.Count > 0)
            .WithMessage("ProductName cannot be null or empty.");

        RuleForEach(request => request.ProductNames)
            .Custom((name, context) =>
            {
                var validation = ProductName.Validate(name!);
                if (validation != Validation.Ok)
                {
                    context.AddFailure(validation.ErrorMessage ?? "ProductName is not valid.");
                }
            });

        RuleFor(request => request.CallbackUri)
            .Must(CallbackUriValidator.IsValidCallbackUri)
            .WithMessage(CallbackUriValidator.InvalidCallbackUriMessage);
    }
}
