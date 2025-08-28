using FluentValidation;
using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for 'updatesSince' request filter format and sinceDateTime presence
/// </summary>
internal class S100UpdateSinceValidator : AbstractValidator<Job>
{
    public S100UpdateSinceValidator()
    {
        RuleFor(request => request.CallbackUri)
            .Must(CallbackUriValidator.IsValidCallbackUri)
            .When(request => !string.IsNullOrEmpty(request.CallbackUri))
            .WithMessage("Invalid callbackUri format.");

        RuleFor(job => job.RequestedFilter)
            .NotEmpty().WithMessage("RequestedFilter cannot be empty for updatesSince requests.")
            .Custom((requestedFilter, context) =>
            {
                var firstColonIndex = requestedFilter.IndexOf(':');
                var sinceDateTime = requestedFilter.Length > firstColonIndex + 1
                    ? requestedFilter.Substring(firstColonIndex + 1)
                    : string.Empty;
                if (!DateTime.TryParseExact(sinceDateTime, "yyyy-MM-ddTHH:mm:ss.fffffffZ", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal, out var parsedDateTime))
                {
                    context.AddFailure("sinceDateTime", $"sinceDateTime '{sinceDateTime}' is not in the required format yyyy-MM-ddTHH:mm:ss.fffffffZ.");
                }
                else
                {
                    foreach (var error in ValidateSinceDateTime(parsedDateTime, sinceDateTime))
                    {
                        context.AddFailure("sinceDateTime", error);
                    }
                }
            });
    }

    private static List<string> ValidateSinceDateTime(DateTime parsedDateTime, string sinceDateTime)
    {
        var errors = new List<string>();
        var nowUtc = DateTime.UtcNow;
        if (parsedDateTime > nowUtc)
        {
            errors.Add($"sinceDateTime '{sinceDateTime}' cannot be a future date.");
        }
        else if (IsMoreThan28DaysInPast(parsedDateTime, nowUtc))
        {
            errors.Add($"sinceDateTime '{sinceDateTime}' cannot be more than 28 days in the past.");
        }
        return errors;
    }

    private static bool IsMoreThan28DaysInPast(DateTime parsedDateTime, DateTime nowUtc)
    {
        return parsedDateTime < nowUtc.AddDays(-28);
    }
}
