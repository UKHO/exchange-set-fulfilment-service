using FluentValidation;
using UKHO.ADDS.EFS.Messages;
using System.Globalization;

namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for 'updatesSince' request filter format and sinceDateTime presence
/// </summary>
internal class S100UpdateSinceValidator : AbstractValidator<S100UpdatesSinceRequest>
{
    public S100UpdateSinceValidator()
    {
        RuleFor(request => request.CallbackUri)
            .Must(CallbackUriValidator.IsValidCallbackUri)
            .WithMessage("Invalid callbackUri format.");

        // Uplifted validation for SinceDateTime property
        RuleFor(request => request.SinceDateTime)
            .NotEqual(default(DateTime))
            .WithMessage("sinceDateTime cannot be empty.")
            .Must(IsValidFormat)
            .WithMessage("sinceDateTime must be in the format yyyy-MM-ddTHH:mm:ss.fffffffZ.")
            .Must(date => !IsFutureDate(date))
            .WithMessage("sinceDateTime cannot be a future date.")
            .Must(date => !IsMoreThan28DaysInPast(date))
            .WithMessage("sinceDateTime cannot be more than 28 days in the past.");

        RuleFor(request => request.ProductIdentifier)
            .Must(ProductIdentifierValidator.IsValid)
            .WithMessage(ProductIdentifierValidator.ValidationMessage);
    }

    private static bool IsValidFormat(DateTime sinceDateTime)
    {
        var sinceDateTimeString = sinceDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
        return DateTime.TryParseExact(
            sinceDateTimeString,
            "yyyy-MM-ddTHH:mm:ss.fffffffZ",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal,
            out _);
    }

    private static bool IsFutureDate(DateTime sinceDateTime)
    {
        return sinceDateTime > DateTime.UtcNow;
    }

    private static bool IsMoreThan28DaysInPast(DateTime sinceDateTime)
    {
        return sinceDateTime < DateTime.UtcNow.AddDays(-28);
    }
}

