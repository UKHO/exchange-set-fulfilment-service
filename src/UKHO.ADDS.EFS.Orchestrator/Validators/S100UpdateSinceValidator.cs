using FluentValidation;
using System.Globalization;
using UKHO.ADDS.EFS.Domain.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for 'updatesSince' request filter format and sinceDateTime presence
/// </summary>
internal class S100UpdateSinceValidator : AbstractValidator<(S100UpdatesSinceRequest s100UpdatesSinceRequest, string? callbackUri, string? productIdentifier)>
{
    private const string ISO_8601_FORMAT = "yyyy-MM-ddTHH:mm:ss.fffffffZ";
    private readonly int _productApiDateTimeLimitDays;

    public S100UpdateSinceValidator(IConfiguration configuration)
    {
        // Read config value from orchestrator section, fallback to 28 if missing
        _productApiDateTimeLimitDays = configuration.GetValue<int>("orchestrator:ProductApiDateTimeLimitDays", 28);

        RuleFor(request => request.callbackUri)
            .Must(CallbackUriValidator.IsValidCallbackUri)
            .WithMessage("Invalid callbackUri format.");

        // Uplifted validation for SinceDateTime property
        RuleFor(request => request.s100UpdatesSinceRequest.SinceDateTime)
            .NotEqual(default(DateTime))
            .WithMessage("sinceDateTime cannot be empty.")
            .Must(date => IsValidISO8601Format(date.ToString(ISO_8601_FORMAT)))
            .WithMessage("sinceDateTime must be in the format"+ ISO_8601_FORMAT+".")
            .Must(date => DateTime.Compare(date, DateTime.UtcNow) <= 0)
            .WithMessage("sinceDateTime cannot be a future date.")
            .Must(date => IsMoreThanXDaysInPast(date, _productApiDateTimeLimitDays))
            .WithMessage($"sinceDateTime cannot be more than {_productApiDateTimeLimitDays} days in the past.");

        RuleFor(request => request.productIdentifier)
            .Must(ProductIdentifierValidator.IsValid)
            .WithMessage(ProductIdentifierValidator.ValidationMessage);
    }

    private static bool IsValidISO8601Format(string sinceDateTimeString)
    {
        return DateTime.TryParseExact(
            sinceDateTimeString,
            ISO_8601_FORMAT,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal,
            out _);
    }

    private static bool IsMoreThanXDaysInPast(DateTime sinceDateTime, int maxDays)
    {
        return DateTime.Compare(sinceDateTime, DateTime.UtcNow.AddDays(-maxDays)) > 0;
    }
}

