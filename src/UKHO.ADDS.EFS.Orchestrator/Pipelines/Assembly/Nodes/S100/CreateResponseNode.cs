using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
//using Link = UKHO.ADDS.EFS.Domain.Files.Link;
//using ExchangeSetLinks = UKHO.ADDS.EFS.Orchestrator.Api.Models.ExchangeSetLinks;
//using CustomExchangeSetResponse = UKHO.ADDS.EFS.Orchestrator.Api.Messages.CustomExchangeSetResponse;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100;

/// <summary>
/// Pipeline node that creates the response for S100 requests
/// </summary>
internal class CreateResponseNode : AssemblyPipelineNode<S100Build>
{
    private readonly ILogger<CreateResponseNode> _logger;

    public CreateResponseNode(AssemblyNodeEnvironment nodeEnvironment,ILogger<CreateResponseNode> logger): base(nodeEnvironment)
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
            
            context.Subject.Job.ExchangeSetUrlExpiryDateTime = DateTime.UtcNow.AddDays(7);
            context.Subject.Job.RequestedProductCount = ProductCount.From(5); // TODO: Replace with actual count
            context.Subject.Job.ExchangeSetProductCount = ProductCount.From(4); // TODO: Replace with actual count
            context.Subject.Job.RequestedProductsAlreadyUpToDateCount = ProductCount.From(1); // TODO: Replace with actual count
            context.Subject.Job.RequestedProductsNotInExchangeSet = new MissingProductList //Dummy data
            {
                new MissingProduct
                {
                    ProductName = ProductName.From("101GB40079ABCDEFG"),
                    Reason = MissingProductReason.InvalidProduct
                }
            };
            context.Subject.Job.BatchId = (BatchId)Guid.NewGuid().ToString("N");

            return NodeResultStatus.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogCreateResponseNodeException((string)correlationId, ex);
            return NodeResultStatus.Failed;
        }
    }
}
