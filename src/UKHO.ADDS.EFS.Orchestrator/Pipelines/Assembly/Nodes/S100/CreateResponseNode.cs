using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
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

    private const string DefaultExchangeSetUrlExpiryDays = "7";
    private const string ExchangeSetUrlExpiryDaysConfigKey = "orchestrator:Response:ExchangeSetUrlExpiryDays";
    private const string FileShareBaseUrlConfigKey = "orchestrator:FileShare:BaseUrl";
    private const string DefaultFileShareBaseUrl = "https://fss.ukho.gov.uk";

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
            // Get the product count metrics - eventually these will come from the context
            var requestedProductCount = job.RequestedProducts?.Count.IsInitialized() == true
                                        ? job.RequestedProducts.Count
                                        : ProductCount.From(0); // Updated to use ProductCount.From(int)
            // TODO: Replace placeholder values with actual counts from context
            var exchangeSetProductCount = build.ProductEditions?.Count() ?? 0;
            var alreadyUpToDateCount = 0;

            var response = CreateResponse(
                job.BatchId,
                requestedProductCount,
                exchangeSetProductCount,
                alreadyUpToDateCount,
                build.MissingProducts);

            // Store the response data for later retrieval
            build.ResponseData = response;

            //_logger.LogInformation(
            //    "Created S100 response. JobId: {JobId}, BatchId: {BatchId}, RequestedProductCount: {RequestedProductCount}, ExchangeSetProductCount: {ExchangeSetProductCount}", 
            //    correlationId, job.BatchId, requestedProductCount, exchangeSetProductCount);

            return NodeResultStatus.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogCreateResponseNodeException((string)correlationId, ex);
            return NodeResultStatus.Failed;
        }
    }

    /// <summary>
    /// Creates a response object for S100 exchange set requests
    /// </summary>
    /// <param name="batchId">The batch ID for the exchange set</param>
    /// <param name="requestedProductCount">Number of products requested</param>
    /// <param name="exchangeSetProductCount">Number of products in the exchange set</param>
    /// <param name="requestedProductsAlreadyUpToDateCount">Number of products already up to date</param>
    /// <param name="missingProducts">List of products that were requested but not included</param>
    /// <returns>A populated S100CustomExchangeSetResponse object</returns>
    private S100CustomExchangeSetResponse CreateResponse(
    BatchId batchId,
    ProductCount requestedProductCount,
    int exchangeSetProductCount,
    int requestedProductsAlreadyUpToDateCount,
    MissingProductList missingProducts)
    {
        // Get the FileShare base URL from configuration or use default
        var fileShareBaseUrl = Environment.Configuration[FileShareBaseUrlConfigKey] ?? DefaultFileShareBaseUrl;

        // Get the URL expiry days from configuration or use default
        if (!int.TryParse(Environment.Configuration[ExchangeSetUrlExpiryDaysConfigKey], out var expiryDays))
        {
            expiryDays = int.Parse(DefaultExchangeSetUrlExpiryDays);
        }

        var expiryDateTime = DateTime.UtcNow.AddDays(expiryDays);

        // Only include ExchangeSetFileUri if we have a valid batchId
        Link? exchangeSetFileUri = null;
        if (batchId != BatchId.None)
        {
            exchangeSetFileUri = new Link { Href = $"{fileShareBaseUrl}/batch/{batchId}/files/exchangeset.zip" };
        }

        // Map MissingProducts to S100ProductNotInExchangeSet
        var requestedProductsNotInExchangeSet = new List<S100ProductNotInExchangeSet>();
        if (missingProducts.HasProducts)
        {
            foreach (var missingProduct in missingProducts.Products)
            {
                requestedProductsNotInExchangeSet.Add(new S100ProductNotInExchangeSet
                {
                    ProductName = missingProduct.ProductName.ToString(),
                    Reason = missingProduct.Reason
                });
            }
        }

        return new S100CustomExchangeSetResponse
        {
            Links = new ExchangeSetLinks
            {
                ExchangeSetBatchStatusUri = new Link { Href = $"{fileShareBaseUrl}/batch/{batchId}/status" },
                ExchangeSetBatchDetailsUri = new Link { Href = $"{fileShareBaseUrl}/batch/{batchId}" },
                ExchangeSetFileUri = exchangeSetFileUri
            },
            ExchangeSetUrlExpiryDateTime = expiryDateTime,
            RequestedProductCount = requestedProductCount.Value, // Explicitly use the Value property of ProductCount
            ExchangeSetProductCount = exchangeSetProductCount,
            RequestedProductsAlreadyUpToDateCount = requestedProductsAlreadyUpToDateCount,
            RequestedProductsNotInExchangeSet = requestedProductsNotInExchangeSet,
            FssBatchId = (string)batchId
        };
    }
}
