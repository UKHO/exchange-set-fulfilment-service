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
                RequestType.UpdatesSince => await ValidateUpdateSinceRequest(job),
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

    private async Task<FluentValidation.Results.ValidationResult> ValidateProductVersionsRequest(Job job)
    {
        // Parse product versions from requested products
        var productVersions = job.RequestedProducts.Names
            .Select(p => p.Value) 
            .Select(p =>
            {
                var parts = p.Split(':'); 
                return new S100ProductVersion
                {
                    ProductName = parts.ElementAtOrDefault(0) ?? string.Empty,
                    EditionNumber = int.TryParse(parts.ElementAtOrDefault(1), out var edition) ? edition : 0,
                    UpdateNumber = int.TryParse(parts.ElementAtOrDefault(2), out var update) ? update : 0
                };
            })
            .ToList();

        var request = new S100ProductVersionsRequest
        {
            ProductVersions = productVersions,
            CallbackUri = job.CallbackUri
        };

        return await _productVersionsRequestValidator.ValidateAsync(request);
    }

    private async Task<FluentValidation.Results.ValidationResult> ValidateUpdateSinceRequest(Job job)
    {
        var firstColonIndex = job.RequestedFilter.IndexOf(':');
        var sinceDateTimeString = job.RequestedFilter.Length > firstColonIndex + 1
            ? job.RequestedFilter.Substring(firstColonIndex + 1)
            : string.Empty;

        var sinceDateTime = DateTime.TryParse(sinceDateTimeString, out var result) ? result : DateTime.MinValue;


        var request = new S100UpdatesSinceRequest
        {
            SinceDateTime = sinceDateTime,
            CallbackUri = job.CallbackUri,
            ProductIdentifier = job.ProductIdentifier,
        };

        return await _updateSinceValidator.ValidateAsync(request);
    }

    private static int GetProductCount(Job job, RequestType requestType)
    {
        return requestType switch
        {
            RequestType.ProductNames => job.RequestedProducts.Names.Count,
            RequestType.ProductVersions => GetProductNamesCountFromVersions(job),
            RequestType.UpdatesSince => 1, // Single date parameter
            _ => 0
        };
    }

    private static int GetProductNamesCountFromVersions(Job job)
    {
        return job.RequestedProducts.Names
            .Select(p => p.Value.Split(':')[0])
            .Count();
    }
}
