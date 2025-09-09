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
            .Custom((callbackUri, context) =>
            {
                if (!CallbackUriValidator.IsValidCallbackUri(callbackUri))
                {
                    context.AddFailure(new ValidationFailure("callbackUri", CallbackUriValidator.INVALID_CALLBACK_URI_MESSAGE));
                }
            });

        RuleFor(request => request.s100UpdatesSinceRequest)
            .Custom((s100UpdatesSinceRequest, context) =>
            {
                if (s100UpdatesSinceRequest == null || !s100UpdatesSinceRequest.SinceDateTime.HasValue)
                {
                    context.AddFailure(new ValidationFailure("sinceDateTime", "No since date time provided."));
                    return;
                }
                var sinceDateTime = s100UpdatesSinceRequest.SinceDateTime.Value;
                if (sinceDateTime.Kind == DateTimeKind.Unspecified)
                {
                    context.AddFailure(new ValidationFailure("sinceDateTime", "Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z')."));
                }
                if (sinceDateTime < DateTime.UtcNow.AddDays(-_maximumProductAge.TotalDays))
                {
                    context.AddFailure(new ValidationFailure("sinceDateTime", $"Date time provided is more than {_maximumProductAge.TotalDays} days in the past."));
                }
                if (!IsNotFutureDate(sinceDateTime))
                {
                    context.AddFailure(new ValidationFailure("sinceDateTime", "sinceDateTime cannot be a future date."));
                }
            });

        RuleFor(request => request.productIdentifier)
            .Must(ProductIdentifierValidator.IsValid)
            .WithMessage(ProductIdentifierValidator.VALIDATION_MESSAGE);
    }

    public async Task<ValidationResult> ValidateAsync((S100UpdatesSinceRequest s100UpdatesSinceRequest, string? callbackUri, string? productIdentifier) request)
    {
        if (request.s100UpdatesSinceRequest == null || !request.s100UpdatesSinceRequest!.SinceDateTime.HasValue)
        {
            return new ValidationResult([new ValidationFailure("sinceDateTime", "No since date time provided.") ]);
        }
        return await base.ValidateAsync(request);
    }

    private static bool IsNotFutureDate(DateTime sinceDateTime)
    {
        return sinceDateTime <= DateTime.UtcNow;
    }
}

