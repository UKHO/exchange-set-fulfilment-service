using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using UKHO.ADDS.EFS.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.Builder.Common.Factories
{
    public class QueueClientFactory
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
            var queueUri = new Uri($"{queueEndpoint}/{queueName}");
            File.WriteAllText(@"C:\Users\ahugob\Desktop\EFS\queueUri.txt", $"{environment}|{queueName}|{queueEndpoint}|{queueUri}"); // For debugging purposes, to see the queue URI being used

            switch (environment)
            {
                case "local":
                    return new QueueClient(queueUri, new StorageSharedKeyCredential(AzuriteAccountName, AzuriteKey));

                default:
                    var clientId = configuration[BuilderEnvironmentVariables.AzureClientId]!;
                    return new QueueClient(queueUri, new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(clientId)));
            }
        }
    }
}
