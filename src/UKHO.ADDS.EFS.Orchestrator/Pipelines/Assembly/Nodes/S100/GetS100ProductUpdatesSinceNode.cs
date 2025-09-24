using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100
{
    internal class GetS100ProductUpdatesSinceNode : AssemblyPipelineNode<S100Build>
    {
        private readonly IProductService _productService;
        private readonly ILogger<GetS100ProductUpdatesSinceNode> _logger;
        private const string ExchangeSetExpiresInConfigKey = "orchestrator:Response:ExchangeSetExpiresIn";

        public GetS100ProductUpdatesSinceNode(AssemblyNodeEnvironment nodeEnvironment, IProductService productService, ILogger<GetS100ProductUpdatesSinceNode> logger)
            : base(nodeEnvironment)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            return Task.FromResult(context.Subject.Job.JobState == JobState.Created
                && (context.Subject.Job.RequestType == RequestType.UpdatesSince));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            var sinceDateTime = job.RequestedFilter;

            var productIdentifier = job.ProductIdentifier;

            ProductEditionList productEditionList;
            var productNameList = build.Products?.Select(p => p.ProductName).ToList() ?? new List<ProductName>();

            try
            {
                productEditionList = await _productService.GetS100ProductUpdatesSinceAsync(sinceDateTime, productIdentifier, job, Environment.CancellationToken);
            }
            catch (Exception)
            {
                await context.Subject.SignalAssemblyError();
                return NodeResultStatus.Failed;
            }

            if (productEditionList?.HasProducts != true)
            {
                await context.Subject.SignalAssemblyError();
                return NodeResultStatus.Failed;
            }

            build.ProductEditions = productEditionList;

            // Get the exchange set expiry duration from configuration
            var expiryTimeSpan = Environment.Configuration.GetValue<TimeSpan>(ExchangeSetExpiresInConfigKey);

            job.ExchangeSetUrlExpiryDateTime = DateTime.UtcNow.Add(expiryTimeSpan);
            job.RequestedProductCount = ProductCount.From(productNameList.Count); //Need to confirm in ESS
            job.ExchangeSetProductCount = productEditionList.Count;
            job.RequestedProductsAlreadyUpToDateCount = productEditionList.ProductCountSummary.RequestedProductsAlreadyUpToDateCount; //Need to confirm in ESS
            job.RequestedProductsNotInExchangeSet = productEditionList.ProductCountSummary.MissingProducts; //Need to confirm in ESS

            await context.Subject.SignalBuildRequired();
            return NodeResultStatus.Succeeded;
        }
    }
}
