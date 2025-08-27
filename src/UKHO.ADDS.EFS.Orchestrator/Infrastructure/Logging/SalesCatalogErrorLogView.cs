using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.VOS;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    internal class SalesCatalogApiErrorLogView
    {
        public DataStandard DataStandard { get; init; }

        public required string Products { get; init; }

        public required CorrelationId CorrelationId { get; init; }

        public static SalesCatalogApiErrorLogView Create(Job job) => new()
        {
            DataStandard = job.DataStandard, Products = job.RequestedProducts, CorrelationId = job.GetCorrelationId()
        };
    }
}
