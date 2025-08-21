using FluentValidation;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Validators;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100;

/// <summary>
/// Pipeline node that validates input parameters for S100 requests
/// </summary>
internal class CreateInputValidationNode : AssemblyPipelineNode<S100Build>
{
    private readonly ILogger<CreateInputValidationNode> _logger;
    private readonly S100ProductNamesRequestValidator _productNamesValidator;

    public CreateInputValidationNode(
        AssemblyNodeEnvironment nodeEnvironment, 
        ILogger<CreateInputValidationNode> logger,
        S100ProductNamesRequestValidator productNamesValidator) 
        : base(nodeEnvironment)
    {
        _logger = logger;
        _productNamesValidator = productNamesValidator;
    }

    public override async Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
    {
        // Always run validation as the first step
        return await Task.FromResult(true);
    }

    protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
    {
        var job = context.Subject.Job;
        var correlationId = job.Id;

        try
        {
            // Determine the request type from the context
            var requestType = GetRequestTypeFromContext(context);

            FluentValidation.Results.ValidationResult validationResult = requestType switch
            {
                RequestType.ProductNames => await ValidateProductNamesRequest(job),
                _ => throw new ArgumentException($"Unsupported request type: {requestType}")
            };

            if (!validationResult.IsValid)
            {
                var validationErrors = validationResult.Errors
                    .Select(error => $"{error.PropertyName}: {error.ErrorMessage}")
                    .ToList();

                _logger.S100InputValidationFailed(
                    correlationId,
                    string.Join("; ", validationErrors));

                // Signal assembly error to set the job state appropriately
                await context.Subject.SignalAssemblyError();

                // Store validation errors in the build for later retrieval
                context.Subject.Build.LogMessages = validationErrors;

                return NodeResultStatus.Failed;
            }

            _logger.S100InputValidationSucceeded(correlationId, GetProductCount(job, requestType));
            
            return NodeResultStatus.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.S100InputValidationError(correlationId, ex);
            await context.Subject.SignalAssemblyError();
            return NodeResultStatus.Failed;
        }
    }

    private static RequestType GetRequestTypeFromContext(IExecutionContext<PipelineContext<S100Build>> context)
    {
        // For now, default to ProductNames as that's the main request type implemented
        // This can be enhanced to detect the actual request type from the context or job properties
        // In the future, this could look at job metadata or other context properties to determine the request type
        return RequestType.ProductNames;
    }

    private async Task<FluentValidation.Results.ValidationResult> ValidateProductNamesRequest(Job job)
    {
        // Parse the requested products from the job
        var productNames = job.RequestedProducts.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        var request = new S100ProductNamesRequest
        {
            ProductNames = productNames
        };

        return await _productNamesValidator.ValidateAsync(request);
    }

    private static int GetProductCount(Job job, RequestType requestType)
    {
        return requestType switch
        {
            RequestType.ProductNames => job.RequestedProducts.Split(',', StringSplitOptions.RemoveEmptyEntries).Length,
            RequestType.ProductVersions => 1, // Would need to be calculated from actual product versions
            RequestType.UpdatesSince => 1, // Single date parameter
            _ => 0
        };
    }
}
