using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Builder.Common.Factories;
using UKHO.ADDS.EFS.Builds;

namespace UKHO.ADDS.EFS.Builder.Common.Pipelines
{
    public abstract class ExchangeSetPipelineContext<TBuild> where TBuild : Build
    {
        private readonly IConfiguration _configuration;

        private readonly QueueClientFactory _queueClientFactory;
        private readonly BlobClientFactory _blobClientFactory;
        private readonly ILoggerFactory _loggerFactory;

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
        }

        public IConfiguration Configuration => _configuration;

        public ILoggerFactory LoggerFactory => _loggerFactory;

        public QueueClientFactory QueueClientFactory => _queueClientFactory;

        public BlobClientFactory BlobClientFactory => _blobClientFactory;

        public string JobId { get; set; }

        public string BatchId { get; set; }

        public string FileShareEndpoint { get; set; }

        public string FileShareHealthEndpoint { get; set; }

        public TBuild Build { get; set; }

        public string ExchangeSetNameTemplate { get; set; }
    }
}
