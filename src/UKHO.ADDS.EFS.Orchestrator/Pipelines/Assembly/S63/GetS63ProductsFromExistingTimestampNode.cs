using UKHO.ADDS.EFS.Jobs.S63;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.S63
{
    internal class GetS63ProductsFromExistingTimestampNode : AssemblyPipelineNode<S63ExchangeSetJob>
    {
        private readonly SalesCatalogueService _salesCatalogueService;

        public GetS63ProductsFromExistingTimestampNode(NodeEnvironment environment, SalesCatalogueService salesCatalogueService)
            : base(environment) =>
            _salesCatalogueService = salesCatalogueService;

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S63ExchangeSetJob> context)
        {
            var job = context.Subject;

            job.Products =
            [
                "An S63 product"
            ];

            return NodeResultStatus.Succeeded;
        }
    }
}
