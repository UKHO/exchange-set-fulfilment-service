using System.Net;
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
    internal class GetProductsForDataStandardNode : AssemblyPipelineNode<S100Build>
    {
        private readonly IProductService _productService;

        public GetProductsForDataStandardNode(AssemblyNodeEnvironment nodeEnvironment, IProductService productService)
            : base(nodeEnvironment)
        {
            _productService = productService;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;

            return Task.FromResult(context.Subject.Job.JobState == JobState.Created && !job.RequestedProducts.HasProducts);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            var (s100SalesCatalogueData, lastModified) = await _productService.GetProductVersionListAsync(DataStandard.S100, job.DataStandardTimestamp, job);

            var nodeResult = NodeResultStatus.NotRun;

            switch (s100SalesCatalogueData.ErrorResponseCode)
            {
                case HttpStatusCode.OK when s100SalesCatalogueData.Products.Any():
                    // We have something to build, so move forwards with scheduling a build
                    build.Products = s100SalesCatalogueData.Products;

                    job.DataStandardTimestamp = lastModified;
                    build.SalesCatalogueTimestamp = lastModified;

                    await context.Subject.SignalBuildRequired();

                    nodeResult = NodeResultStatus.Succeeded;

                    break;
                case HttpStatusCode.NotModified:
                    // No new data since the specified timestamp, so no build needed
                    job.DataStandardTimestamp = lastModified;

                    await context.Subject.SignalNoBuildRequired();

                    nodeResult = NodeResultStatus.Succeeded;

                    break;
                default:
                    // Something went wrong, so the job has failed
                    await context.Subject.SignalAssemblyError();

                    nodeResult = NodeResultStatus.Failed;

                    break;
            }

            return nodeResult;
        }
    }
}
