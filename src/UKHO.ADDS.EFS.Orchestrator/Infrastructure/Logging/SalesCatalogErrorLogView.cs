using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    internal class SalesCatalogApiErrorLogView
    {
        public ExchangeSetDataStandard DataStandard { get; init; }

        public required string Products { get; init; }

        public required string CorrelationId { get; init; }

        public static SalesCatalogApiErrorLogView Create(ExchangeSetJob job) =>
            new() { DataStandard = job.DataStandard, Products = job.GetProductDelimitedList(), CorrelationId = job.GetCorrelationId() };
    }
}
