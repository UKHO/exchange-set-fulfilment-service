using FluentValidation.Results;
using UKHO.ADDS.EFS.Domain.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Validators.S100;

public interface IS100UpdateSinceRequestValidator
{
    Task<ValidationResult> ValidateAsync((S100UpdatesSinceRequest s100UpdatesSinceRequest, string? callbackUri, string? productIdentifier) request);
}
