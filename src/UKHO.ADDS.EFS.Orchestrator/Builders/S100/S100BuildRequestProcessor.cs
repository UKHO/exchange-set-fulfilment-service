using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Builders.S100
{
    internal class S100BuildRequestProcessor : BuildRequestProcessor
    {
        public S100BuildRequestProcessor(QueueServiceClient queueServiceClient, IConfiguration configuration)
            : base(queueServiceClient, configuration)
        {
        }

        public override async Task SendBuildRequestAsync(ExchangeSetJob job, CancellationToken stoppingToken)
        {
            const string requestQueueName = StorageConfiguration.S100BuildRequestQueueName;

            var queueClient = QueueServiceClient.GetQueueClient(requestQueueName);

            var request = new BuildRequest
            {
                Version = 1,
                Timestamp = DateTime.UtcNow,

                JobId = job.Id,
                BatchId = job.BatchId,
                DataStandard = job.DataStandard,
                FileShareServiceUri = Configuration["Endpoints:S100BuilderFileShare"]!,
                WorkspaceKey = Configuration["IICWorkspaceKey"]!,
                ExchangeSetNameTemplate = Configuration["ExchangeSetNameTemplate"]!
            };

            var messageJson = JsonCodec.Encode(request);
            await queueClient.SendMessageAsync(messageJson, stoppingToken);
        }
    }
}
