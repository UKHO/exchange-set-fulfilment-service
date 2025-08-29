using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
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
                RequestType.ProductVersions => new FluentValidation.Results.ValidationResult(),
                RequestType.UpdatesSince => new FluentValidation.Results.ValidationResult(),
                _ => throw new ArgumentException($"Unsupported request type: {requestType}")
            };

            if (!validationResult.IsValid)
            {
                var validationErrors = validationResult.Errors
                    .Select(error => $"{error.ErrorMessage}")
                    .ToList();

                _logger.S100InputValidationFailed(
                    (string)correlationId,
                    string.Join("; ", validationErrors));

                // Signal assembly error to set the job state appropriately
                await context.Subject.SignalAssemblyError();

                // Store validation errors in the build for later retrieval
                context.Subject.Build.LogMessages = validationErrors;

                // Populate ErrorResponseModel with validation errors
                context.Subject.ErrorResponse = new ErrorResponseModel
                {
                    CorrelationId = (string)correlationId,
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
            _logger.S100InputValidationSucceeded((string)correlationId, GetProductCount(job, (RequestType)requestType));

            return NodeResultStatus.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.S100InputValidationError((string)correlationId, ex);
            await context.Subject.SignalAssemblyError();
            return NodeResultStatus.Failed;
        }
    }

    /// <summary>
    /// Validates ProductNames request by parsing job data and running FluentValidation
    /// </summary>
    /// <param name="job">The job containing request data</param>
    /// <returns>FluentValidation result</returns>
    private async Task<FluentValidation.Results.ValidationResult> ValidateProductNamesRequest(Job job)
    {
        // Extract product names from ProductNameList
        var productNames = job.RequestedProducts.Names
            .Select(p => (string)p)
            .ToList();

        var request = new S100ProductNamesRequest
        {
            ProductNames = productNames,
            CallbackUri = job.CallbackUri
        };

        return await _productNamesValidator.ValidateAsync(request);
    }

    /// <summary>
    /// Gets the count of products for different request types
    /// </summary>
    /// <param name="job">The job containing request data</param>
    /// <param name="requestType">The type of request</param>
    /// <returns>Product count</returns>
    private static int GetProductCount(Job job, RequestType requestType)
    {
        return requestType switch
        {
            RequestType.ProductNames => job.RequestedProducts.Names.Count,
            RequestType.ProductVersions => 1, // Would need to be calculated from actual product versions
            RequestType.UpdatesSince => 1, // Single date parameter
            _ => 0
        };
    }
}
