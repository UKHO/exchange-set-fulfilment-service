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
        RuleFor(job => job.RequestedFilter)
            .NotEmpty().WithMessage("RequestedFilter cannot be empty for updatesSince requests.")
            .Custom((requestedFilter, context) =>
            {
                var firstColonIndex = requestedFilter.IndexOf(':');

                var sinceDateTime = requestedFilter.Length > firstColonIndex + 1
                    ? requestedFilter.Substring(firstColonIndex + 1)
                    : string.Empty;
                if (!DateTime.TryParseExact(sinceDateTime, "yyyy-MM-ddTHH:mm:ss.fffffffZ", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal, out _))
                {
                    context.AddFailure("sinceDateTime", $"sinceDateTime '{sinceDateTime}' is not in the required format yyyy-MM-ddTHH:mm:ss.fffffffZ.");
                }
            });
    }
}
