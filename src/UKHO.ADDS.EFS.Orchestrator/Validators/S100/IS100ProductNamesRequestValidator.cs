using FluentValidation.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Validators.S100;

internal interface IS100ProductNamesRequestValidator
{
    Task<ValidationResult> ValidateAsync((List<string>? productNamesRequest, string? callbackUri) request);
}
