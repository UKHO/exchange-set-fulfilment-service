using UKHO.ADDS.EFS.Builds.S63;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S63
{
    internal class GetProductsForDataStandardNode : AssemblyPipelineNode<S63Build>
    {
        private readonly IOrchestratorSalesCatalogueClient _salesCatalogueClient;

        public GetProductsForDataStandardNode(AssemblyNodeEnvironment nodeEnvironment, IOrchestratorSalesCatalogueClient salesCatalogueClient)
            : base(nodeEnvironment)
        {
            _salesCatalogueClient = salesCatalogueClient;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S63Build>> context)
        {
            return Task.FromResult(context.Subject.Job.JobState == JobState.Created);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S63Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            build.Products = ["An S63 Product"];

            await context.Subject.SignalBuildRequired();

            return NodeResultStatus.Succeeded;
        }
    }
}
