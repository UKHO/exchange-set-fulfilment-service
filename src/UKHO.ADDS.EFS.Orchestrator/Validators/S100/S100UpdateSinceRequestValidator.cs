using FluentValidation;
using FluentValidation.Results;
using System.Globalization;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Validators.S100
{
    /// <summary>
    /// Validator for 'updatesSince' request filter format and sinceDateTime presence
    /// </summary>
    internal class S100UpdateSinceRequestValidator : AbstractValidator<(UpdatesSinceRequest? s100UpdatesSinceRequest, string? callbackUri, string? productIdentifier)>, IS100UpdateSinceRequestValidator
    {
        private const string InvalidUpdateSinceDateFormatMessage = "Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z')";
        private readonly TimeSpan _maximumProductAge;

        public S100UpdateSinceRequestValidator(IConfiguration configuration)
        {
            _maximumProductAge = configuration.GetValue("orchestrator:MaximumProductAge", TimeSpan.FromDays(28));

            RuleFor(request => request.callbackUri)
                .Custom((callbackUri, context) =>
                {
                    if (!CallbackUriValidator.IsValidCallbackUri(callbackUri))
                    {
                        context.AddFailure(new ValidationFailure("callbackUri", CallbackUriValidator.InvalidCallbackUriMessage));
                    }
                });
            RuleFor(request => request.s100UpdatesSinceRequest)
                .Custom((s100UpdatesSinceRequest, context) =>
                {
                    var sinceDateTimeStr = s100UpdatesSinceRequest?.SinceDateTime;
                    if (string.IsNullOrWhiteSpace(sinceDateTimeStr))
                    {
                        context.AddFailure(new ValidationFailure("sinceDateTime", "No UpdateSince date time provided"));
                        return;
                    }

                    if (!DateTime.TryParse(sinceDateTimeStr, null, DateTimeStyles.RoundtripKind, out var sinceDateTime))
                    {
                        context.AddFailure(new ValidationFailure("sinceDateTime", InvalidUpdateSinceDateFormatMessage));
                        return;
                    }

                    if (sinceDateTime.Kind == DateTimeKind.Unspecified)
                    {
                        context.AddFailure(new ValidationFailure("sinceDateTime", InvalidUpdateSinceDateFormatMessage));
                    }

                    if (sinceDateTime < DateTime.UtcNow.AddDays(-_maximumProductAge.TotalDays))
                    {
                        context.AddFailure(new ValidationFailure("sinceDateTime", $"Date time provided is more than {_maximumProductAge.TotalDays} days in the past"));
                    }

                    if (!IsNotFutureDate(sinceDateTime))
                    {
                        context.AddFailure(new ValidationFailure("sinceDateTime", "UpdateSince date cannot be a future date"));
                    }
                });
            RuleFor(request => request.productIdentifier)
                .Custom((productIdentifier, context) =>
                {
                    if (!ProductIdentifierValidator.IsValid(productIdentifier))
                    {
                        context.AddFailure(new ValidationFailure("productIdentifier", ProductIdentifierValidator.ValidationMessage));
                    }
                });
        }

        public async Task<ValidationResult> ValidateAsync((UpdatesSinceRequest updatesSinceRequest, string? callbackUri, string? productIdentifier) request)
        {
            return await base.ValidateAsync(request);
        }

        private static bool IsNotFutureDate(DateTime sinceDateTime)
        {
            return sinceDateTime <= DateTime.UtcNow;
        }
    }
}

