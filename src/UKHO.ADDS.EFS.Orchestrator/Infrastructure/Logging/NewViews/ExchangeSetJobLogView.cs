using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.NewViews
{
    internal class ExchangeSetJobLogView
    {
        public required string Id { get; init; }

        public required string BatchId { get; init; }

        public DateTime Timestamp { get; init; }

        public DateTime? SalesCatalogueTimestamp { get; init; }

        public ExchangeSetJobState State { get; init; }

        public ExchangeSetDataStandard DataStandard { get; init; }

        public int ProductCount { get; init; }

        public static ExchangeSetJobLogView Create(ExchangeSetJob job)
        {
            return new ExchangeSetJobLogView()
            {
                Id = job.Id,
                BatchId = job.BatchId,
                Timestamp = job.Timestamp,
                SalesCatalogueTimestamp = job.SalesCatalogueTimestamp,
                State = job.State,
                DataStandard = job.DataStandard,
                ProductCount = job.GetProductCount()
            };
        }
    }
}
