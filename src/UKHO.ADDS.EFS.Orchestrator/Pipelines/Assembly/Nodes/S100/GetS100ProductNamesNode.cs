using System.Net;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100
{
    internal class GetS100ProductNamesNode : AssemblyPipelineNode<S100Build>
    {
        private readonly IOrchestratorSalesCatalogueClient _salesCatalogueClient;
        private readonly ILogger<GetS100ProductNamesNode> _logger;

        public GetS100ProductNamesNode(AssemblyNodeEnvironment nodeEnvironment, IOrchestratorSalesCatalogueClient salesCatalogueClient, ILogger<GetS100ProductNamesNode> logger)
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

            var productNames = build.Products?
                .Select(p => p.ProductName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray() ?? [];

            var s100SalesCatalogueData = await _salesCatalogueClient.GetS100ProductNamesAsync(productNames, job, Environment.CancellationToken);

            var nodeResult = NodeResultStatus.NotRun;

            switch (s100SalesCatalogueData.ResponseCode)
            {
                case HttpStatusCode.OK when s100SalesCatalogueData.Products.Any():

                    if (s100SalesCatalogueData.ProductCounts.ReturnedProductCount == 0)
                    {
                        await context.Subject.SignalAssemblyError();
                        _logger.LogSalesCatalogueProductsNotReturned(SalesCatalogServiceProductsNotReturnedView.Create(s100SalesCatalogueData.ProductCounts));
                        return NodeResultStatus.Failed;
                    }

                    // Log any requested products that weren't returned, but don't fail the build
                    if (s100SalesCatalogueData.ProductCounts.RequestedProductsNotReturned.Count > 0)
                    {
                        _logger.LogSalesCatalogueProductsNotReturned(SalesCatalogServiceProductsNotReturnedView.Create(s100SalesCatalogueData.ProductCounts));
                    }

                    build.ProductNames = s100SalesCatalogueData.Products;

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
