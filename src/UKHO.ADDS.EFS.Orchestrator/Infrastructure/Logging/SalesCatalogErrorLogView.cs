using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    internal class SalesCatalogApiErrorLogView
    {
        public DataStandard DataStandard { get; init; }

        public required string Products { get; init; }

        public required string CorrelationId { get; init; }

        public static SalesCatalogApiErrorLogView Create(Build job) =>
            new() { DataStandard = job.DataStandard, Products = job.GetProductDelimitedList(), CorrelationId = job.GetCorrelationId() };
    }
}
