using FluentValidation.Results;
using UKHO.ADDS.EFS.Messages;
using System.Collections.Generic;

namespace UKHO.ADDS.EFS.Orchestrator.Validators.S100;

public interface IS100ProductNamesRequestValidator
{
    Task<ValidationResult> ValidateAsync(S100ProductNamesRequest request);
}
