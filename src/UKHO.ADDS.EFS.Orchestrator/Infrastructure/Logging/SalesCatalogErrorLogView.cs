using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    internal class SalesCatalogApiErrorLogView
    {
        public DataStandard DataStandard { get; init; }

        public required ProductNameList Products { get; init; }

        public required CorrelationId CorrelationId { get; init; }

        public static SalesCatalogApiErrorLogView Create(Job job) => new()
        {
            DataStandard = job.DataStandard,
            Products = job.RequestedProducts,
            CorrelationId = job.GetCorrelationId()
        };
    }
}
