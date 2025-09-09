using System.Net;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100
{
    internal class newGetS100ProductNamesNode : AssemblyPipelineNode<S100Build>
    {
        private readonly IOrchestratorSalesCatalogueClient _salesCatalogueClient;
        private readonly ILogger<GetS100ProductNamesNode> _logger;

        public newGetS100ProductNamesNode(AssemblyNodeEnvironment nodeEnvironment, IOrchestratorSalesCatalogueClient salesCatalogueClient, ILogger<GetS100ProductNamesNode> logger)
            : base(nodeEnvironment)
        {
            _salesCatalogueClient = salesCatalogueClient ?? throw new ArgumentNullException(nameof(salesCatalogueClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            return Task.FromResult(context.Subject.Job.JobState == JobState.Created);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            var productNames = job.RequestedProducts.Names;

            var s100SalesCatalogueData = await _salesCatalogueClient.GetS100ProductEditionListAsync(productNames, job, Environment.CancellationToken);

            var nodeResult = NodeResultStatus.NotRun;

            switch (s100SalesCatalogueData.ResponseCode)
            {
                case HttpStatusCode.OK:

                    if (s100SalesCatalogueData.ProductCountSummary.ReturnedProductCount == 0)
                    {
                        await context.Subject.SignalAssemblyError();
                        _logger.LogSalesCatalogueProductsNotReturned(s100SalesCatalogueData.ProductCountSummary);
                        return NodeResultStatus.Failed;
                    }

                    // Log any requested products that weren't returned, but don't fail the build
                    if (s100SalesCatalogueData.ProductCountSummary.MissingProducts.Count > 0)
                    {
                        _logger.LogSalesCatalogueProductsNotReturned(s100SalesCatalogueData.ProductCountSummary);
                    }

                    build.ProductEditions = s100SalesCatalogueData.Products;

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
