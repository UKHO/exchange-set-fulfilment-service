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
    internal class GetS100ProductNamesNode : AssemblyPipelineNode<S100Build>
    {
        private readonly IProductService _productService;
        private readonly ILogger<GetS100ProductNamesNode> _logger;

        public GetS100ProductNamesNode(AssemblyNodeEnvironment nodeEnvironment, IProductService productService, ILogger<GetS100ProductNamesNode> logger)
            : base(nodeEnvironment)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            return Task.FromResult(context.Subject.Job.JobState == JobState.Created && (context.Subject.Job.RequestType == RequestType.ProductNames || context.Subject.Job.RequestType == RequestType.Internal));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            var productNameList = new List<ProductName>();

            if (job.RequestedProducts.HasProducts)
            {
                productNameList.AddRange(job.RequestedProducts);
            }
            else
            {
                productNameList.AddRange(build.Products?.Select(p => p.ProductName) ?? []);
            }

            var productEditionList = await _productService.GetProductEditionListAsync(DataStandard.S100, productNameList, job, Environment.CancellationToken);

            var nodeResult = NodeResultStatus.NotRun;

            switch (productEditionList.ResponseCode)
            {
                case HttpStatusCode.OK:

                    // Log any requested products that weren't returned, but don't fail the build
                    if (productEditionList.ProductCountSummary.MissingProducts.HasProducts)
                    {
                        _logger.LogSalesCatalogueProductsNotReturned(productEditionList.ProductCountSummary);
                    }

                    if (job.RequestType == RequestType.ProductNames)
                    {
                        job.RequestedProductCount = ProductCount.From(productNameList.Count);
                        job.ExchangeSetProductCount = productEditionList.Count;
                        job.RequestedProductsAlreadyUpToDateCount = productEditionList.ProductCountSummary.RequestedProductsAlreadyUpToDateCount;
                        job.RequestedProductsNotInExchangeSet = productEditionList.ProductCountSummary.MissingProducts;
                    }

                    build.ProductEditions = productEditionList.Products;

                    await context.Subject.SignalBuildRequired();

                    return NodeResultStatus.Succeeded;

                default:
                    await context.Subject.SignalAssemblyError();

                    nodeResult = NodeResultStatus.Failed;

                    break;
            }

            return nodeResult;
        }
    }
}
