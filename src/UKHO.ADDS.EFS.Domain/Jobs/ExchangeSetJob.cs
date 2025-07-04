using UKHO.ADDS.EFS.Messages;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// TODO Check init;

namespace UKHO.ADDS.EFS.Jobs
{
    public abstract class ExchangeSetJob
    {
        /// <summary>
        ///     The job id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The timestamp of the job creation.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     The Sales Catalogue timestamp queried for this job.
        /// </summary>
        public DateTime? SalesCatalogueTimestamp { get; set; }

        /// <summary>
        ///     The existing timestamp we have stored against this data standard.
        /// </summary>
        public DateTime? ExistingTimestamp { get; set; }

        /// <summary>
        ///     The job state.
        /// </summary>
        public ExchangeSetJobState State { get; set; }

        /// <summary>
        ///     The job data standard, which indicates the format of the data being processed.
        /// </summary>
        public ExchangeSetDataStandard DataStandard { get; set; }

        /// <summary>
        ///     The FSS Batch ID associated with the job.
        /// </summary>
        public string BatchId { get; set; }

        /// <summary>
        ///     Gets the correlation ID for the job.
        /// </summary>
        /// <remarks>This is always the Job ID.</remarks>
        /// <returns></returns>
        public string GetCorrelationId() => Id;

        /// <summary>
        ///     Gets a list of products in a delimited format.
        /// </summary>
        /// <returns></returns>
        public abstract string GetProductDelimitedList();

        /// <summary>
        ///     Gets the count of products in the job.
        /// </summary>
        /// <returns></returns>
        public abstract int GetProductCount();
    }
}
