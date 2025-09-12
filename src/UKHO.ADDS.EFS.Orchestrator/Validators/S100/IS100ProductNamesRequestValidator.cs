using FluentValidation.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Validators.S100;

public interface IS100ProductNamesRequestValidator
{
    Task<ValidationResult> ValidateAsync((List<string>? productNames, string? callbackUri) request);
}
