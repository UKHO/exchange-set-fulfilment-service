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
using UKHO.ADDS.Clients.Common.Constants;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100
{
    internal class GetS100ProductVersionsNode : AssemblyPipelineNode<S100Build>
    {
        private readonly IProductService _productService;
        private readonly ILogger<GetS100ProductVersionsNode> _logger;

        public GetS100ProductVersionsNode(AssemblyNodeEnvironment nodeEnvironment, IProductService productService, ILogger<GetS100ProductVersionsNode> logger)
            : base(nodeEnvironment)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            return Task.FromResult(context.Subject.Job.JobState == JobState.Created &&
                context.Subject.Job.ExchangeSetType == ExchangeSetType.ProductVersions);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;
            var productVersions = job.ProductVersions;
            var scsResponse = context.Subject.ResponseInfo;

            // Call the product service to get product versions
            ProductEditionList productEditionList;
            try
            {
                productEditionList = await _productService.GetProductVersionsListAsync(DataStandard.S100, productVersions, job, Environment.CancellationToken);

                scsResponse.ResponseCode = productEditionList.ResponseCode;
                scsResponse.LastModified = productEditionList.LastModified;
                scsResponse.ServiceName = ServiceNameType.SCS;
            }
            catch (Exception)
            {
                await context.Subject.SignalAssemblyError();
                return NodeResultStatus.Failed;
            }

            if (productEditionList.ResponseCode == HttpStatusCode.OK || productEditionList.ResponseCode == HttpStatusCode.NotModified)
            {
                // Log any requested products that weren't returned, but don't fail the build
                if (productEditionList.ProductCountSummary.MissingProducts.HasProducts)
                {
                    _logger.LogSalesCatalogueProductsNotReturned(productEditionList.ProductCountSummary);
                }

                build.ProductEditions = productEditionList.Products;

                job.RequestedProductCount = ProductCount.From(productVersions.Count());
                job.ExchangeSetProductCount = productEditionList.Count;
                if (productEditionList.ResponseCode == HttpStatusCode.NotModified)
                {
                    job.RequestedProductsAlreadyUpToDateCount = ProductCount.From(productVersions.Count());
                }
                else
                {
                    job.RequestedProductsAlreadyUpToDateCount = productEditionList.ProductCountSummary.RequestedProductsAlreadyUpToDateCount.IsInitialized()
                        ? productEditionList.ProductCountSummary.RequestedProductsAlreadyUpToDateCount
                        : ProductCount.From(0);
                }
                job.RequestedProductsNotInExchangeSet = productEditionList.ProductCountSummary.MissingProducts;

                await context.Subject.SignalBuildRequired();
                return NodeResultStatus.Succeeded;
            }

            // Handle error case
            await context.Subject.SignalAssemblyError();
            return NodeResultStatus.Failed;
        }
    }
}
