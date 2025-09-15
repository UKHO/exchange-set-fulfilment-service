using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Messages;

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
        /// The response data for successful S100 requests
        /// </summary>
        internal S100CustomExchangeSetResponse? ResponseData { get; set; }

        /// <summary>
        ///     Gets the correlation ID for the job.
        /// </summary>
        /// <remarks>This is always the Job ID.</remarks>
        /// <returns></returns>
        public CorrelationId GetCorrelationId() => CorrelationId.From((string)Id);

        /// <summary>
        /// The URI to be called back when the job is completed.
        /// </summary>
        public string? CallbackUri { get; init; }

        /// <summary>
        /// The identifier for the product associated with the job, if applicable.
        /// </summary>
        public string? ProductIdentifier { get; init; }
    }
}
