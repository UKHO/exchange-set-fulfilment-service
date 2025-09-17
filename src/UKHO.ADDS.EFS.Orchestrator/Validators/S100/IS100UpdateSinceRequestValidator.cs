using FluentValidation.Results;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Validators.S100
{
    internal interface IS100UpdateSinceRequestValidator
    {
        Task<ValidationResult> ValidateAsync((UpdatesSinceRequest updatesSinceRequest, string? callbackUri, string? productIdentifier) request);
    }
}
