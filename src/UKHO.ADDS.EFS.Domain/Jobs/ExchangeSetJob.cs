using UKHO.ADDS.EFS.Messages;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// TODO Check init;

namespace UKHO.ADDS.EFS.Jobs
{
    public abstract class ExchangeSetJob
    {
        public string Id { get; set; }

        public DateTime Timestamp { get; set; }

        public DateTime? SalesCatalogueTimestamp { get; set; }

        public ExchangeSetJobState State { get; set; }

        public ExchangeSetDataStandard DataStandard { get; set; }

        public string BatchId { get; set; }

        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets a list of products in a delimited format.
        /// </summary>
        /// <returns></returns>
        public abstract string GetProductDelimitedList();

        /// <summary>
        /// Gets the count of products in the job.
        /// </summary>
        /// <returns></returns>
        public abstract int GetProductCount();
    }
}
