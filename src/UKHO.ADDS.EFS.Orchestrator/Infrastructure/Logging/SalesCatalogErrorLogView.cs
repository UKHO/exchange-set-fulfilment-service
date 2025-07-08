using UKHO.ADDS.EFS.NewEFS;
using UKHO.ADDS.EFS.NewEFS.S100;

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
