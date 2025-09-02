using FluentValidation;
using UKHO.ADDS.EFS.Messages;
using System.Globalization;

namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for 'updatesSince' request filter format and sinceDateTime presence
/// </summary>
internal class S100UpdateSinceValidator : AbstractValidator<S100UpdatesSinceRequest>
{
    private const string ISO_8601_FORMAT = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

    public S100UpdateSinceValidator()
    {
        RuleFor(request => request.CallbackUri)
            .Must(CallbackUriValidator.IsValidCallbackUri)
            .WithMessage("Invalid callbackUri format.");

        // Uplifted validation for SinceDateTime property
        RuleFor(request => request.SinceDateTime)
            .NotEqual(default(DateTime))
            .WithMessage("sinceDateTime cannot be empty.")
            .Must(IsValidISO8601Format)
            .WithMessage("sinceDateTime must be in the format"+ ISO_8601_FORMAT+".")
            .Must(date => DateTime.Compare(date, DateTime.UtcNow) <= 0)
            .WithMessage("sinceDateTime cannot be a future date.")
            .Must(date => IsMoreThan28DaysInPast(date))
            .WithMessage("sinceDateTime cannot be more than 28 days in the past.");

        RuleFor(request => request.ProductIdentifier)
            .Must(ProductIdentifierValidator.IsValid)
            .WithMessage(ProductIdentifierValidator.ValidationMessage);
    }

    private static bool IsValidISO8601Format(DateTime sinceDateTime)
    {
        var sinceDateTimeString = sinceDateTime.ToString(ISO_8601_FORMAT);
        return DateTime.TryParseExact(
            sinceDateTimeString,
            ISO_8601_FORMAT,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal,
            out _);
    }

    private static bool IsMoreThan28DaysInPast(DateTime sinceDateTime)
    {
        return DateTime.Compare(sinceDateTime, DateTime.UtcNow.AddDays(-Convert.ToInt32(28))) > 0;
    }
}

