using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.ExternalErrors;
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

        public GetS100ProductUpdatesSinceNode(AssemblyNodeEnvironment nodeEnvironment, IProductService productService)
            : base(nodeEnvironment)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            return Task.FromResult(context.Subject.Job.JobState == JobState.Created
                && (context.Subject.Job.ExchangeSetType == ExchangeSetType.UpdatesSince));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            var sinceDateTime = job.RequestedFilter;

            var productIdentifier = job.ProductIdentifier;

            ProductEditionList productEditionList;
            ExternalServiceError? externalServiceError;
            var productNameList = build.Products?.Select(p => p.ProductName).ToList() ?? new List<ProductName>();

            try
            {
                (productEditionList, externalServiceError) = await _productService.GetS100ProductUpdatesSinceAsync(sinceDateTime, productIdentifier, job, Environment.CancellationToken);

                if (externalServiceError != null)
                {
                    job.ExternalServiceError = new ExternalServiceError(
                        externalServiceError.ErrorResponseCode,
                        externalServiceError.ServiceName
                    );
                }

                job.ProductsLastModified = productEditionList.ProductsLastModified ?? DateTime.UtcNow;
            }
            catch (Exception)
            {
                await context.Subject.SignalAssemblyError();
                return NodeResultStatus.Failed;
            }

            var externalApiResponseCode = job.ExternalServiceError?.ErrorResponseCode ?? System.Net.HttpStatusCode.OK;

            var evaluation = await EvaluateScsResponseAsync(productEditionList, externalServiceError!, context);
            if (evaluation != NodeResultStatus.Succeeded ||
                (evaluation == NodeResultStatus.Succeeded && externalApiResponseCode == System.Net.HttpStatusCode.NotModified))
            {
                return evaluation;
            }

            build.ProductEditions = productEditionList;

            job.RequestedProductCount = ProductCount.From(productNameList.Count);
            job.ExchangeSetProductCount = productEditionList.Count;
            job.RequestedProductsAlreadyUpToDateCount = productEditionList.ProductCountSummary.RequestedProductsAlreadyUpToDateCount;
            job.RequestedProductsNotInExchangeSet = productEditionList.ProductCountSummary.MissingProducts;

            await context.Subject.SignalBuildRequired();
            return NodeResultStatus.Succeeded;
        }

        private static async Task<NodeResultStatus> EvaluateScsResponseAsync(ProductEditionList productEditionList, ExternalServiceError externalServiceError, IExecutionContext<PipelineContext<S100Build>> context)
        {
            if (externalServiceError != null && externalServiceError.ErrorResponseCode == System.Net.HttpStatusCode.NotModified)
            {
                await context.Subject.SignalNoBuildRequired();
                return NodeResultStatus.Succeeded;
            }

            if (!productEditionList.HasProducts)
            {
                await context.Subject.SignalAssemblyError();
                return NodeResultStatus.Failed;
            }

            return NodeResultStatus.Succeeded;
        }
    }
}
