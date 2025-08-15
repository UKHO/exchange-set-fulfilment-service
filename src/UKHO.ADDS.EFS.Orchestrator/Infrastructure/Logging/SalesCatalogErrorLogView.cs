using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    internal class SalesCatalogApiErrorLogView
    {
        public DataStandard DataStandard { get; init; }

        public required string Products { get; init; }

        public required string CorrelationId { get; init; }

        public static SalesCatalogApiErrorLogView Create(Job job) =>
            new() { DataStandard = job.DataStandard, Products = string.Join(", ", job.RequestedProducts ?? []), CorrelationId = job.GetCorrelationId() };
    }
}
