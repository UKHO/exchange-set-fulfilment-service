using FluentValidation;
using FluentValidation.Results;
using UKHO.ADDS.EFS.Domain.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Validators.S100;

/// <summary>
/// Validator for 'updatesSince' request filter format and sinceDateTime presence
/// </summary>
internal class S100UpdateSinceRequestValidator : AbstractValidator<(S100UpdatesSinceRequest? s100UpdatesSinceRequest, string? callbackUri, string? productIdentifier)>, IS100UpdateSinceRequestValidator
{
    private readonly TimeSpan _maximumProductAge;

    public S100UpdateSinceRequestValidator(IConfiguration configuration)
    {
        _maximumProductAge = configuration.GetValue("orchestrator:MaximumProductAge", TimeSpan.FromDays(28));

        RuleFor(request => request.callbackUri)
            .Must(CallbackUriValidator.IsValidCallbackUri)
            .WithMessage(CallbackUriValidator.INVALID_CALLBACK_URI_MESSAGE);

        RuleFor(date => date.s100UpdatesSinceRequest!.SinceDateTime)
            .Must(sinceDateTime => sinceDateTime!.Value.Kind != DateTimeKind.Unspecified)
            .WithMessage("Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z').");

        RuleFor(date => date.s100UpdatesSinceRequest!.SinceDateTime)
            .GreaterThan(DateTime.UtcNow.AddDays(-_maximumProductAge.TotalDays))
            .WithMessage($"Date time provided is more than {_maximumProductAge.TotalDays} days in the past.");

        RuleFor(date => date.s100UpdatesSinceRequest!.SinceDateTime)
            .Must(dateStr => IsNotFutureDate(dateStr))
            .WithMessage($"sinceDateTime cannot be a future date.");

        RuleFor(request => request.productIdentifier)
            .Must(ProductIdentifierValidator.IsValid)
            .WithMessage(ProductIdentifierValidator.VALIDATION_MESSAGE);
    }

    public async Task<ValidationResult> ValidateAsync((S100UpdatesSinceRequest s100UpdatesSinceRequest, string? callbackUri, string? productIdentifier) request)
    {

        if (request.s100UpdatesSinceRequest == null || !request.s100UpdatesSinceRequest!.SinceDateTime.HasValue)
        {
            return new ValidationResult([new ValidationFailure(nameof(request.s100UpdatesSinceRequest.SinceDateTime), "No since date time provided.") ]);
        }

        return await base.ValidateAsync(request);
    }

    private static bool IsNotFutureDate(DateTime? sinceDateTime)
    {
        if (sinceDateTime == null) return true;
        return sinceDateTime.Value <= DateTime.UtcNow;
    }
}

