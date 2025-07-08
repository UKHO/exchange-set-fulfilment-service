﻿using System.Net;
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
                    // Products were successfully retrieved
                    var filteredProducts = result.s100SalesCatalogueData.ResponseBody
                        .Where(p => p.ProductName != null && p.ProductName.StartsWith(job.ProductIdentifier, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    job.Products = filteredProducts;
                    job.SalesCatalogueTimestamp = result.LastModified;
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
