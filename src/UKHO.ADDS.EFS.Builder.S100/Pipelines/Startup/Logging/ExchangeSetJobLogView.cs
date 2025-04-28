using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Messages;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup.Logging
{
    internal class ExchangeSetJobLogView
    {
        public string Id { get; set; }

        public DateTime Timestamp { get; set; }

        public DateTime? SalesCatalogueTimestamp { get; set; }

        public ExchangeSetJobState State { get; set; }

        public ExchangeSetDataStandard DataStandard { get; set; }

        public int ProductCount { get; set; }

        public static ExchangeSetJobLogView CreateFromJob(ExchangeSetJob job)
        {
            return new ExchangeSetJobLogView()
            {
                Id = job.Id,
                Timestamp = job.Timestamp,
                SalesCatalogueTimestamp = job.SalesCatalogueTimestamp,
                State = job.State,
                DataStandard = job.DataStandard,
                ProductCount = job.Products?.Count ?? 0
            };
        }
    }
}
