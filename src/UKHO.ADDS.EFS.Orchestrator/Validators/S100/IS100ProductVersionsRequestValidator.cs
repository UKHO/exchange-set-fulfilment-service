using FluentValidation.Results;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Validators.S100;

internal interface IS100ProductVersionsRequestValidator
{
    Task<ValidationResult> ValidateAsync((IEnumerable<ProductVersionRequest>? productVersionsRequest, string? callbackUri) request);
}
