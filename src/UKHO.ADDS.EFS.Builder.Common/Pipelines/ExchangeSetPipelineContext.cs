using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.Common.Factories;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.EFS.Builder.Common.Pipelines
{
    public abstract class ExchangeSetPipelineContext<TJob, TSummary> where TJob : ExchangeSetJob where TSummary : BuildSummary, new()
    {
        private readonly IConfiguration _configuration;

        private readonly QueueClientFactory _queueClientFactory;
        private readonly BlobClientFactory _blobClientFactory;
        private readonly ILoggerFactory _loggerFactory;

        private readonly TSummary _summary;

        private string _jobId;
        private string _batchId;

        protected ExchangeSetPipelineContext(
            IConfiguration configuration,
            QueueClientFactory queueClientFactory,
            BlobClientFactory blobClientFactory,
            ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _queueClientFactory = queueClientFactory;
            _blobClientFactory = blobClientFactory;
            _loggerFactory = loggerFactory;

            _jobId = string.Empty;
            _batchId = string.Empty;

            _summary = new TSummary();
        }

        public IConfiguration Configuration => _configuration;

        public ILoggerFactory LoggerFactory => _loggerFactory;

        public TSummary Summary => _summary;

        public QueueClientFactory QueueClientFactory => _queueClientFactory;

        public BlobClientFactory BlobClientFactory => _blobClientFactory;

        public string JobId
        {
            get => _jobId;
            set
            {
                _jobId = value;
                _summary.JobId = value;
            }
        }

        public string BatchId
        {
            get => _batchId;
            set
            {
                _batchId = value;
                _summary.BatchId = value;
            }
        }

        public string FileShareEndpoint { get; set; }

        public string FileShareHealthEndpoint { get; set; }

        public TJob Job { get; set; }

        public string ExchangeSetNameTemplate { get; set; }
    }
}
