using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100;

/// <summary>
/// Pipeline node that creates the response for S100 requests
/// </summary>
internal class CreateResponseNode : AssemblyPipelineNode<S100Build>
{
    private readonly ILogger<CreateResponseNode> _logger;

    public CreateResponseNode(
        AssemblyNodeEnvironment nodeEnvironment,
        ILogger<CreateResponseNode> logger)
        : base(nodeEnvironment)
    {
        _logger = logger;
    }

    public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
    {
        return Task.FromResult(context.Subject.Job.JobState == JobState.Created);
    }

    protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
    {
        var job = context.Subject.Job;
        var correlationId = job.Id;

        try
        {
            var response = CreateResponse(5, 4, 1); // TODO: Replace with actual counts from build context
            
            context.Subject.Build.ResponseData = response;

            return NodeResultStatus.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogCreateResponseNodeException((string)correlationId, ex);
            return NodeResultStatus.Failed;
        }
    }

    // Moved from S100CustomExchangeSetApiRouteBuilderExtension
    private static S100CustomExchangeSetResponse CreateResponse(
        int requestedProductCount,
        int exchangeSetProductCount,
        int requestedProductsAlreadyUpToDateCount)
    {
        var batchId = Guid.NewGuid().ToString("N"); // Simulate batch ID for demonstration purposes
        var jobId = Guid.NewGuid().ToString("N"); // Use correlation ID as job ID

        return new S100CustomExchangeSetResponse
        {
            Links = new S100ExchangeSetLinks
            {
                ExchangeSetBatchStatusUri = new S100Link { Href = $"http://fss.ukho.gov.uk/batch/{batchId}/status" },
                ExchangeSetBatchDetailsUri = new S100Link { Href = $"http://fss.ukho.gov.uk/batch/{batchId}" },
                ExchangeSetFileUri = batchId != null ? new S100Link { Href = $"http://fss.ukho.gov.uk/batch/{batchId}/files/exchangeset.zip" } : null
            },
            ExchangeSetUrlExpiryDateTime = DateTime.UtcNow.AddDays(7), // TODO: Get from configuration
            RequestedProductCount = requestedProductCount,
            ExchangeSetProductCount = exchangeSetProductCount,
            RequestedProductsAlreadyUpToDateCount = requestedProductsAlreadyUpToDateCount,
            RequestedProductsNotInExchangeSet =
            [
                new S100ProductNotInExchangeSet
                {
                    ProductName = "101GB40079ABCDEFG",
                    Reason = S100ProductNotIncludedReason.InvalidProduct
                }
            ],
            FssBatchId = batchId
        };
    }
}
