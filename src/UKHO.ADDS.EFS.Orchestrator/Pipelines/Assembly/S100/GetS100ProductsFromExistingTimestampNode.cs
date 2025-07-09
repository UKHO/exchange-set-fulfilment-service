using System.Net;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.S100
{
    internal class GetS100ProductsFromExistingTimestampNode : AssemblyPipelineNode<S100ExchangeSetJob>
    {
        private readonly SalesCatalogueService _salesCatalogueService;

        public GetS100ProductsFromExistingTimestampNode(NodeEnvironment environment, SalesCatalogueService salesCatalogueService)
            : base(environment) =>
            _salesCatalogueService = salesCatalogueService;

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S100ExchangeSetJob> context)
        {
            var job = context.Subject;

            var result = await _salesCatalogueService.GetS100ProductsFromSpecificDateAsync(job.ExistingTimestamp, job);

            switch (result.s100SalesCatalogueData.ResponseCode)
            {
                case HttpStatusCode.OK when result.s100SalesCatalogueData.ResponseBody.Any():
                    var filteredProducts = FilterProducts(result.s100SalesCatalogueData.ResponseBody, job.ProductIdentifier);
                    job.Products = filteredProducts;
                    job.SalesCatalogueTimestamp = result.LastModified;
                    break;

                case HttpStatusCode.NotModified:
                    job.State = ExchangeSetJobState.Cancelled;
                    job.SalesCatalogueTimestamp = result.LastModified;
                    break;

                default:
                    job.State = ExchangeSetJobState.Cancelled;
                    job.SalesCatalogueTimestamp = job.ExistingTimestamp;
                    break;
            }

            return NodeResultStatus.Succeeded;
        }

        private static List<S100Products> FilterProducts(IEnumerable<S100Products> products, string productIdentifier)
        {
            if (string.IsNullOrWhiteSpace(productIdentifier))
            {
                return products.Where(p => !string.IsNullOrWhiteSpace(p.ProductName)).ToList();
            }

            return products
                .Where(p => !string.IsNullOrWhiteSpace(p.ProductName) &&
                            p.ProductName.StartsWith(productIdentifier, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
