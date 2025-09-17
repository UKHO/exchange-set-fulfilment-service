using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;

namespace UKHO.ADDS.EFS.Domain.Builds
{
    public abstract class Build
    {
        private List<string> _logMessages;
        private List<BuildNodeStatus> _statuses;
        private MissingProductList _missingProducts;

        protected Build()
        {
            _statuses = [];
            _logMessages = [];
            _missingProducts = new MissingProductList();
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
        /// Gets or sets the build commit information containing file details with hash values.
        /// </summary>
        public BuildCommitInfo BuildCommitInfo { get; init; }

        /// <summary>
        ///     Gets or sets the list of products that were requested but couldn't be included in the build.
        /// </summary>
        public MissingProductList MissingProducts
        {
            get => _missingProducts;
            set => _missingProducts = value ?? new MissingProductList();
        }

        /// <summary>
        ///     Gets or sets the count of requested products that are already up to date.
        /// </summary>
        public ProductCount RequestedProductsAlreadyUpToDateCounts { get; set; }

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
