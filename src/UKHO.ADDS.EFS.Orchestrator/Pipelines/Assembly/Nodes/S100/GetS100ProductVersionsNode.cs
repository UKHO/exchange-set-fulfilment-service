using System.Net;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100
{
    internal class GetS100ProductVersionsNode : AssemblyPipelineNode<S100Build>
    {
        private readonly IProductService _productService;
        private readonly ILogger<GetS100ProductVersionsNode> _logger;
        private const string ExchangeSetExpiresInConfigKey = "orchestrator:Response:ExchangeSetExpiresIn";

        public GetS100ProductVersionsNode(AssemblyNodeEnvironment nodeEnvironment, IProductService productService, ILogger<GetS100ProductVersionsNode> logger)
            : base(nodeEnvironment)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            return Task.FromResult(context.Subject.Job.JobState == JobState.Created &&
                context.Subject.Job.RequestType == RequestType.ProductVersions);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;
            var productVersions = job.ProductVersions;

            // Call the product service to get product versions
            var productEditionList = await _productService.GetProductVersionsListAsync(DataStandard.S100, productVersions, job, Environment.CancellationToken);

            job.ScsResponseCode = productEditionList.ResponseCode;
            job.ScsLastModified = productEditionList.LastModified;

            if (productEditionList.ResponseCode == HttpStatusCode.OK || productEditionList.ResponseCode == HttpStatusCode.NotModified)
            {
                // Log any requested products that weren't returned, but don't fail the build
                if (productEditionList.ProductCountSummary.MissingProducts.HasProducts)
                {
                    _logger.LogSalesCatalogueProductsNotReturned(productEditionList.ProductCountSummary);
                }

                build.ProductEditions = productEditionList.Products;

                // Get the exchange set expiry duration from configuration
                var expiryTimeSpan = Environment.Configuration.GetValue<TimeSpan>(ExchangeSetExpiresInConfigKey);

                job.ExchangeSetUrlExpiryDateTime = DateTime.UtcNow.Add(expiryTimeSpan);
                // Ensure ProductCount value objects are initialized before any persistence/serialization
                job.RequestedProductCount = ProductCount.From(productVersions.Count());
                job.ExchangeSetProductCount = ProductCount.From(productEditionList.Count());
                job.RequestedProductsAlreadyUpToDateCount = productEditionList.ProductCountSummary.RequestedProductsAlreadyUpToDateCount;
                job.RequestedProductsNotInExchangeSet = productEditionList.ProductCountSummary.MissingProducts;

                await context.Subject.SignalBuildRequired();
                return NodeResultStatus.Succeeded;
            }
            else
            {
                job.ErrorOrigin = "SCS";
            }

            // Handle error case
            await context.Subject.SignalAssemblyError();
            return NodeResultStatus.Failed;
        }
    }
}
