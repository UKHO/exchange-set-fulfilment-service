using FluentValidation.Results;
using UKHO.ADDS.EFS.Domain.Messages;
using System.Collections.Generic;

namespace UKHO.ADDS.EFS.Orchestrator.Validators.S100;

public interface IS100ProductVersionsRequestValidator
{
    Task<ValidationResult> ValidateAsync((IEnumerable<S100ProductVersion> productVersions, string? callbackUri) request);
}
