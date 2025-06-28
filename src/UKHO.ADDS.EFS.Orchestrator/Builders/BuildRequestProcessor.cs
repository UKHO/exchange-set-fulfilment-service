using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Builders
{
    internal abstract class BuildRequestProcessor
    {
        private readonly QueueServiceClient _queueServiceClient;
        private readonly IConfiguration _configuration;

        protected BuildRequestProcessor(QueueServiceClient queueServiceClient, IConfiguration configuration)
        {
            _queueServiceClient = queueServiceClient;
            _configuration = configuration;
        }

        protected IConfiguration Configuration => _configuration;

        protected QueueServiceClient QueueServiceClient => _queueServiceClient;

        public abstract Task SendBuildRequestAsync(ExchangeSetJob job, CancellationToken stoppingToken);
    }
}
