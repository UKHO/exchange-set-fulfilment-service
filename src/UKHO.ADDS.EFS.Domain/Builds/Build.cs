using UKHO.ADDS.EFS.External;
using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.EFS.Builds
{
    public abstract class Build
    {
        private List<string> _logMessages;
        private List<BuildNodeStatus> _statuses;

        protected Build()
        {
            _statuses = [];
            _logMessages = [];
        }

        public JobId JobId { get; init; }

        /// <summary>
        ///     The Sales Catalogue timestamp queried for this job.
        /// </summary>
        public DateTime? SalesCatalogueTimestamp { get; set; }

        /// <summary>
        ///     The build data standard, which indicates the format of the data being processed.
        /// </summary>
        public DataStandard DataStandard { get; init; }

        /// <summary>
        ///     The FSS Batch ID associated with the build.
        /// </summary>
        public BatchId BatchId { get; set; }

        /// <summary>
        ///     Gets the collection of node statuses for this build.
        /// </summary>
        public IEnumerable<BuildNodeStatus>? Statuses
        {
            get => _statuses;
            set => _statuses = value?.ToList() ?? [];
        }

        /// <summary>
        ///     Gets the collection of log messages for this build.
        /// </summary>
        public IEnumerable<string>? LogMessages
        {
            get => _logMessages;
            set => _logMessages = value?.ToList() ?? [];
        }

        /// <summary>
        ///     Gets the correlation ID for the build.
        /// </summary>
        /// <remarks>This is always the Job ID.</remarks>
        /// <returns></returns>
        public CorrelationId GetCorrelationId() => CorrelationId.From((string)JobId);

        /// <summary>
        ///     Gets a list of products in a delimited format.
        /// </summary>
        /// <returns></returns>
        public abstract string GetProductDelimitedList();

        /// <summary>
        ///     Gets a lexically ordered string that represents the products.
        /// </summary>
        /// <returns></returns>
        public abstract string GetProductDiscriminant();

        /// <summary>
        ///     Gets the count of products in the job.
        /// </summary>
        /// <returns></returns>
        public abstract int GetProductCount();

        /// <summary>
        ///     Adds node statuses and logs to the build
        /// </summary>
        /// <param name="statuses"></param>
        /// <param name="logMessages"></param>
        public void SetOutputs(IEnumerable<BuildNodeStatus> statuses, IEnumerable<string> logMessages)
        {
            _statuses.AddRange(statuses);

            _logMessages ??= [];
            _logMessages.AddRange(logMessages);
        }
    }
}
