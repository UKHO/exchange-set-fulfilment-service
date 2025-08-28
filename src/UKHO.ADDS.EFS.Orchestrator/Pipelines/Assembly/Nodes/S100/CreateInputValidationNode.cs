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
    private readonly S100ProductVersionsRequestValidator _productVersionsRequestValidator;
    private readonly S100UpdateSinceValidator _updateSinceValidator;

    public CreateInputValidationNode(
        AssemblyNodeEnvironment nodeEnvironment,
        ILogger<CreateInputValidationNode> logger,
        S100ProductNamesRequestValidator productNamesValidator,
        S100ProductVersionsRequestValidator productVersionsRequestValidator,
        S100UpdateSinceValidator updateSinceValidator)
        : base(nodeEnvironment)
    {
        _logger = logger;
        _productNamesValidator = productNamesValidator;
        _productVersionsRequestValidator = productVersionsRequestValidator;
        _updateSinceValidator = updateSinceValidator;
    }

    public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
    {
        return Task.FromResult(context.Subject.Job.JobState == JobState.Created);
    }

    protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
    {
        var job = context.Subject.Job;
        var correlationId = job.Id;
        var requestType = context.Subject.RequestType;

        try
        {
            FluentValidation.Results.ValidationResult validationResult = requestType switch
            {
                RequestType.ProductNames => await ValidateProductNamesRequest(job),
                RequestType.ProductVersions => await ValidateProductVersionsRequest(job),
                RequestType.UpdatesSince => await _updateSinceValidator.ValidateAsync(job),
                _ => throw new ArgumentException($"Unsupported request type: {requestType}")
            };

            if (!validationResult.IsValid)
            {
                var validationErrors = validationResult.Errors
                    .Select(error => $"{error.ErrorMessage}")
                    .ToList();

                _logger.S100InputValidationFailed(
                    correlationId,
                    string.Join("; ", validationErrors));

                // Signal assembly error to set the job state appropriately
                await context.Subject.SignalAssemblyError();

                // Store validation errors in the build for later retrieval
                context.Subject.Build.LogMessages = validationErrors;

                // Populate ErrorResponseModel with validation errors
                context.Subject.ErrorResponse = new ErrorResponseModel
                {
                    CorrelationId = correlationId,
                    Errors = validationResult.Errors
                        .Select(e => new ErrorDetail
                        {
                            Source = e.PropertyName,
                            Description = e.ErrorMessage
                        })
                        .ToList()
                };

                return NodeResultStatus.Failed;
            }

            // Explicitly cast requestType to non-nullable RequestType
            _logger.S100InputValidationSucceeded(correlationId, GetProductCount(job, (RequestType)requestType));

            return NodeResultStatus.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.S100InputValidationError(correlationId, ex);
            await context.Subject.SignalAssemblyError();
            return NodeResultStatus.Failed;
        }
    }

    private async Task<FluentValidation.Results.ValidationResult> ValidateProductNamesRequest(Job job)
    {
        var productNames = job.RequestedProducts.Split(',', StringSplitOptions.None)
            .Select(p => p.Trim())
            .ToList();

        var request = new S100ProductNamesRequest
        {
            ProductNames = productNames,
            CallbackUri = job.CallbackUri
        };

        return await _productNamesValidator.ValidateAsync(request);
    }

    private async Task<FluentValidation.Results.ValidationResult> ValidateProductVersionsRequest(Job job)
    {
        // Parse product versions from requested products
        var productVersions = job.RequestedProducts.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        // Extract product names from product versions (first part before ':')
        var productNames = productVersions
            .Select(p => p.Split(':')[0])
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        var productNamesRequest = new S100ProductNamesRequest
        {
            ProductNames = productNames,
            CallbackUri = job.CallbackUri
        };

        var productNamesValidationResult = await _productNamesValidator.ValidateAsync(productNamesRequest);
        if (!productNamesValidationResult.IsValid)
        {
            return productNamesValidationResult;
        }

        var request = new ProductVersionsRequest
        {
            ProductVersions = productVersions
        };

        return await _productVersionsRequestValidator.ValidateAsync(request);
    }

    private static int GetProductCount(Job job, RequestType requestType)
    {
        return requestType switch
        {
            RequestType.ProductNames => job.RequestedProducts.Split(',', StringSplitOptions.RemoveEmptyEntries).Length,
            RequestType.ProductVersions => GetProductNamesCountFromVersions(job),
            RequestType.UpdatesSince => 1, // Single date parameter
            _ => 0
        };
    }

    private static int GetProductNamesCountFromVersions(Job job)
    {
        return job.RequestedProducts
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => p.Split(':')[0])
            .Count();
    }
}
