using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
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
    private const string ExchangeSetUrlExpiryDaysConfigKey = "orchestrator:Response:ExchangeSetUrlExpiryDays";

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
        var build = context.Subject.Build;
        var correlationId = job.Id;

        try
        {
            var requestedProductCount = job.RequestedProducts?.Count ?? ProductCount.From(0);
            var exchangeSetProductCount = build.ProductEditions?.Count() != null
                ? ProductCount.From(build.ProductEditions.Count())
                : ProductCount.From(0);

            // Get and parse the expiry days configuration
            if (!int.TryParse(Environment.Configuration[ExchangeSetUrlExpiryDaysConfigKey], out int expiryDays))
            {
                var configValue = Environment.Configuration[ExchangeSetUrlExpiryDaysConfigKey];
                throw new InvalidOperationException(
                    $"Invalid configuration value for {ExchangeSetUrlExpiryDaysConfigKey}: {configValue ?? "<null>"}");
            }

            context.Subject.Job.ExchangeSetUrlExpiryDateTime = DateTime.UtcNow.AddDays(expiryDays);
            context.Subject.Job.RequestedProductCount = requestedProductCount;
            context.Subject.Job.ExchangeSetProductCount = exchangeSetProductCount;
            context.Subject.Job.RequestedProductsAlreadyUpToDateCount = build.RequestedProductsAlreadyUpToDateCounts;
            context.Subject.Job.RequestedProductsNotInExchangeSet = build.MissingProducts;

            return NodeResultStatus.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogCreateResponseNodeException((string)correlationId, ex);
            return NodeResultStatus.Failed;
        }
    }
}
