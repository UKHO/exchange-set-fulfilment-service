using System.Net;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.S100
{
    internal class GetS100ProductNamesNode : AssemblyPipelineNode<S100ExchangeSetJob>
    {
        private readonly SalesCatalogueService _salesCatalogueService;

        public GetS100ProductNamesNode(NodeEnvironment environment, SalesCatalogueService salesCatalogueService)
            : base(environment) =>
            _salesCatalogueService = salesCatalogueService;

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S100ExchangeSetJob> context)
        {
            var job = context.Subject;

            var productNames = job.GetProductDelimitedList()
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(name => name.Trim())
                .ToArray();

            var result = await _salesCatalogueService.GetS100ProductNamesAsync(productNames, job, Environment.CancellationToken);

            switch (result.s100SalesCatalogueData.ResponseCode)
            {
                case HttpStatusCode.OK when result.s100SalesCatalogueData.Products.Any():
                    // Products were successfully retrieved
                    job.S100ProductNames = result.s100SalesCatalogueData.Products;
                    job.SalesCatalogueTimestamp = result.LastModified; // TODO Check - this seems to be null
                    break;

                case HttpStatusCode.NotModified:
                    // No new data since the specified timestamp
                    job.State = ExchangeSetJobState.Cancelled;
                    job.SalesCatalogueTimestamp = result.LastModified;
                    break;

                default:
                    // Any other response code (error cases)
                    job.State = ExchangeSetJobState.Cancelled;
                    job.SalesCatalogueTimestamp = job.ExistingTimestamp;
                    break;
            }

            return NodeResultStatus.Succeeded;
        }
    }
}
