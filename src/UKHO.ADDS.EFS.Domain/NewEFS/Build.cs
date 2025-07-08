using UKHO.ADDS.EFS.Builds;

namespace UKHO.ADDS.EFS.NewEFS
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

        public required string JobId { get; init; }

        /// <summary>
        ///     The Sales Catalogue timestamp queried for this job.
        /// </summary>
        public DateTime? SalesCatalogueTimestamp { get; set; }

        /// <summary>
        ///     The build data standard, which indicates the format of the data being processed.
        /// </summary>
        public required DataStandard DataStandard { get; init; }

        /// <summary>
        ///     The state of the build.
        /// </summary>
        public required BuildState BuildState { get; init; }

        /// <summary>
        ///     The FSS Batch ID associated with the build.
        /// </summary>
        public required string BatchId { get; init; }

        /// <summary>
        ///     Gets the collection of node statuses for this build.
        /// </summary>
        public IEnumerable<BuildNodeStatus>? Statuses
        {
            get => _statuses;
            internal set => _statuses = value?.ToList() ?? [];
        }

        /// <summary>
        ///     Gets the collection of log messages for this build.
        /// </summary>
        public IEnumerable<string>? LogMessages
        {
            get => _logMessages;
            internal set => _logMessages = value?.ToList() ?? [];
        }

        /// <summary>
        ///     Gets the correlation ID for the build.
        /// </summary>
        /// <remarks>This is always the Job ID.</remarks>
        /// <returns></returns>
        public string GetCorrelationId() => JobId;

        /// <summary>
        ///     Gets a list of products in a delimited format.
        /// </summary>
        /// <returns></returns>
        public abstract string GetProductDelimitedList();

        /// <summary>
        ///     Gets a lexically ordered string that represents the products.
        /// </summary>
        /// <returns></returns>
        public abstract string GetProductDiscriminator();

        /// <summary>
        ///     Gets the count of products in the job.
        /// </summary>
        /// <returns></returns>
        public abstract int GetProductCount();

        /// <summary>
        ///     Adds a node status to the build. Each builder node records a status after execution.
        /// </summary>
        /// <param name="status"></param>
        public void AddStatus(BuildNodeStatus status)
        {
            _statuses ??= [];
            _statuses.Add(status);
        }

        /// <summary>
        ///     Sets the collection of log messages for the build.
        /// </summary>
        /// <param name="logLines"></param>
        public void SetLogMessages(IEnumerable<string> logLines)
        {
            _logMessages ??= [];
            _logMessages.AddRange(logLines);
        }
    }
}
