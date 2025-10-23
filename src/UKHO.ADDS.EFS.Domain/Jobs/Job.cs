using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.ExternalErrors;
using UKHO.ADDS.EFS.Domain.Products;

namespace UKHO.ADDS.EFS.Domain.Jobs
{
    public partial class Job
    {
        public Job()
        {
            JobState = JobState.Created;
            BuildState = BuildState.None;
        }

        public void DoIt()
        {
            JobState = JobState.Completed;
            BuildState = BuildState.Succeeded;
        }

        /// <summary>
        ///     The job id.
        /// </summary>
        public required JobId Id { get; init; }

        /// <summary>
        ///     The timestamp of the job creation.
        /// </summary>
        public required DateTime Timestamp { get; init; }

        /// <summary>
        ///  The last known timestamp for the data standard associated with the job as reported by Sales Catalogue Service,
        /// or DateTime.MinValue if not available.
        /// </summary>
        public DateTime? DataStandardTimestamp { get; set; }

        /// <summary>
        ///     The job data standard, which indicates the format of the data being processed.
        /// </summary>
        public required DataStandard DataStandard { get; init; }

        /// <summary>
        ///     The current state of the job.
        /// </summary>
        [JsonInclude]
        public JobState JobState { get; private set; }

        /// <summary>
        ///     The current build state.
        /// </summary>
        [JsonInclude]
        public BuildState BuildState { get; private set; }

        /// <summary>
        ///     Gets the requested products for the job.
        /// </summary>
        public required ProductNameList RequestedProducts { get; init; }

        /// <summary>
        ///     Gets the requested filter for the job.
        /// </summary>
        public required string RequestedFilter { get; init; }

        /// <summary>
        ///     The FSS Batch ID associated with the job.
        /// </summary>
        public BatchId BatchId { get; set; }

        /// <summary>
        /// The expiry date and time for the exchange set URL
        /// </summary>
        public DateTime ExchangeSetUrlExpiryDateTime { get; set; }

        /// <summary>
        /// Number of products explicitly requested
        /// </summary>
        public ProductCount RequestedProductCount { get; set; } = ProductCount.None;

        /// <summary>
        /// Number of products that have data included in the produced Exchange Set
        /// </summary>
        public ProductCount ExchangeSetProductCount { get; set; } = ProductCount.None;

        /// <summary>
        /// Number of requested products that are already up to date
        /// </summary>
        public ProductCount RequestedProductsAlreadyUpToDateCount { get; set; } = ProductCount.None;

        /// <summary>
        /// Products that were requested but not included in the exchange set
        /// </summary>
        public MissingProductList RequestedProductsNotInExchangeSet { get; set; } = new();

        /// <summary>
        /// Deternimes the type of exchange set to be created
        /// </summary>
        public ExchangeSetType ExchangeSetType { get; init; }

        /// <summary>
        ///     Gets the correlation ID for the job.
        /// </summary>
        /// <remarks>This is always the Job ID.</remarks>
        /// <returns></returns>
        public CorrelationId GetCorrelationId() => CorrelationId.From((string)Id);

        /// <summary>
        /// The URI to be called back when the job is completed.
        /// </summary>
        public CallbackUri CallbackUri { get; init; }

        /// <summary>
        /// The identifier for the product associated with the job, if applicable.
        /// </summary>
        public DataStandardProduct ProductIdentifier { get; init; }

        public ProductVersionList ProductVersions { get; init; }

        public DateTime ProductsLastModified { get; set; }

        public ExternalServiceError ExternalServiceError { get; set; } = new ExternalServiceError(System.Net.HttpStatusCode.OK, ExternalServiceName.NotDefined);
    }
}
