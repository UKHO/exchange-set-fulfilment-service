using System.Globalization;
using System.Net;
using FluentValidation;
using UKHO.ADDS.EFS.Domain.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Validators.S100;

/// <summary>
/// Validator for 'updatesSince' request filter format and sinceDateTime presence
/// </summary>
internal class S100UpdateSinceRequestValidator : AbstractValidator<(S100UpdatesSinceRequest s100UpdatesSinceRequest, string? callbackUri, string? productIdentifier)>, IS100UpdateSinceRequestValidator
{
    private const string ISO_8601_FORMAT = "yyyy-MM-ddTHH:mm:ss.fffffffZ";
    private readonly TimeSpan _maximumProductAge;

    public S100UpdateSinceRequestValidator(IConfiguration configuration)
    {
        _maximumProductAge = configuration.GetValue("orchestrator:MaximumProductAge", TimeSpan.FromDays(28));

        RuleFor(request => request.callbackUri)
            .Must(CallbackUriValidator.IsValidCallbackUri)
            .WithMessage(CallbackUriValidator.InvalidCallbackUriMessage);

        RuleFor(request => request.s100UpdatesSinceRequest.SinceDateTime)
            .NotEmpty()
            .WithMessage("sinceDateTime cannot be empty.")
            .Must(IsValidISO8601Format)
            .WithMessage("sinceDateTime must be in the format " + ISO_8601_FORMAT + ".")
            .Must(dateStr => IsNotFutureDate(dateStr))
            .WithMessage("sinceDateTime cannot be a future date.")
            .WithErrorCode(HttpStatusCode.NotModified.ToString())
            .Must(dateStr => IsNotTooOld(dateStr, _maximumProductAge))
            .WithMessage($"sinceDateTime cannot be older than {_maximumProductAge.TotalDays:0} days in the past.");

        RuleFor(request => request.productIdentifier)
            .Must(ProductIdentifierValidator.IsValid)
            .WithMessage(ProductIdentifierValidator.ValidationMessage);
    }

    public async Task<FluentValidation.Results.ValidationResult> ValidateAsync((S100UpdatesSinceRequest s100UpdatesSinceRequest, string? callbackUri, string? productIdentifier) request)
    {
        return await base.ValidateAsync(request);
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

    private static bool IsNotFutureDate(string sinceDateTimeString)
    {
        if (!DateTime.TryParseExact(sinceDateTimeString, ISO_8601_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var date))
            return true;
        return DateTime.Compare(date, DateTime.UtcNow) <= 0;
    }

    private static bool IsNotTooOld(string sinceDateTimeString, TimeSpan maxAge)
    {
        if (!DateTime.TryParseExact(sinceDateTimeString, ISO_8601_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var date))
            return true;
        return date >= DateTime.UtcNow.Subtract(maxAge);
    }
}

