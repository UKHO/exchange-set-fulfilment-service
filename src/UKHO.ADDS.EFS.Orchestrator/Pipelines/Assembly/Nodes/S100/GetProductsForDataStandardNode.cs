using System.Net;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using static Quartz.Logging.OperationName;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100
{
    internal class GetProductsForDataStandardNode : AssemblyPipelineNode<S100Build>
    {
        private readonly IOrchestratorSalesCatalogueClient _salesCatalogueClient;

        public GetProductsForDataStandardNode(AssemblyNodeEnvironment nodeEnvironment, IOrchestratorSalesCatalogueClient salesCatalogueClient)
            : base(nodeEnvironment)
        {
            _salesCatalogueClient = salesCatalogueClient;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;

            return Task.FromResult(context.Subject.Job.JobState == JobState.Created && (job.RequestedProducts?.Length == 0 || job.RequestedProducts == null));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            var (s100SalesCatalogueData, lastModified) = await _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(job.DataStandardTimestamp, job);

            var nodeResult = NodeResultStatus.NotRun;

            switch (s100SalesCatalogueData.ResponseCode)
            {
                case HttpStatusCode.OK when s100SalesCatalogueData.ResponseBody.Any():
                    // We have something to build, so move forwards with scheduling a build
                    build.Products = s100SalesCatalogueData.ResponseBody;

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
