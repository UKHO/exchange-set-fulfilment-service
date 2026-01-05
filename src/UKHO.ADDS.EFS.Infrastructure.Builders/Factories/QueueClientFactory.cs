using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.Infrastructure.Builders.Factories
{
    public class QueueClientFactory : IQueueClientFactory
    {
        public const string AzuriteAccountName = "devstoreaccount1";
        private const string AzuriteKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

        public QueueClient CreateRequestQueueClient(IConfiguration configuration)
        {
            var requestQueueName = configuration[BuilderEnvironmentVariables.RequestQueueName]!;
            return CreateQueueClient(configuration, requestQueueName);
        }

        public QueueClient CreateResponseQueueClient(IConfiguration configuration)
        {
            var responseQueueName = configuration[BuilderEnvironmentVariables.ResponseQueueName]!;
            return CreateQueueClient(configuration, responseQueueName);
        }

        private static QueueClient CreateQueueClient(IConfiguration configuration, string queueName)
        {
            var environment = configuration[BuilderEnvironmentVariables.AddsEnvironment]!;
            var queueEndpoint = configuration[BuilderEnvironmentVariables.QueueEndpoint]!;
            var queueUri = queueEndpoint.EndsWith('/') ? new Uri($"{queueEndpoint}{queueName}") : new Uri($"{queueEndpoint}/{queueName}");

            switch (environment)
            {
                case "local":
                    return new QueueClient(queueUri, new StorageSharedKeyCredential(AzuriteAccountName, AzuriteKey));
                default:
                    var clientId = configuration[BuilderEnvironmentVariables.AzureClientId]!;
                    var credential = new ManagedIdentityCredential(clientId);
                    return new QueueClient(queueUri, credential);
            }
        }
    }
}
