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
            var environment = configuration[BuilderEnvironmentVariables.AddsEnvironment]!;
            var queueConnectionString = configuration[BuilderEnvironmentVariables.QueueConnectionString]!;
            var requestQueueName = configuration[BuilderEnvironmentVariables.RequestQueueName]!;
            Console.WriteLine($"Factory request Queue Client Start :{queueConnectionString}/{requestQueueName} "); // rhz:
            switch (environment)
            {
                case "local":
                    // We need to construct the client using the "URL" method for running locally so that the container network can connect to Azurite using host.docker.internal
                    var queueUri = new Uri($"{queueConnectionString}/{requestQueueName}");

                    Console.WriteLine($"Factory req queue uri (local) :{queueConnectionString}/{requestQueueName} "); // rhz:

                    return new QueueClient(queueUri, new StorageSharedKeyCredential(AzuriteAccountName, AzuriteKey));

                default:
                    Console.WriteLine($"Factory req queue params (default) :{queueConnectionString}, {requestQueueName} "); // rhz:
                    return new QueueClient(queueConnectionString, requestQueueName);
            }
        }

        public QueueClient CreateResponseQueueClient(IConfiguration configuration)
        {
            var environment = Environment.GetEnvironmentVariable(BuilderEnvironmentVariables.AddsEnvironment)!;
            var queueConnectionString = configuration[BuilderEnvironmentVariables.QueueConnectionString]!;
            var responseQueueName = configuration[BuilderEnvironmentVariables.ResponseQueueName]!;
            Console.WriteLine($"Factory response Queue Client Start :{queueConnectionString}/{responseQueueName} "); // rhz:
            switch (environment)
            {
                case "local":
                    // We need to construct the client using the "URL" method for running locally so that the container network can connect to Azurite using host.docker.internal
                    var queueUri = new Uri($"{queueConnectionString}/{responseQueueName}");
                    Console.WriteLine($"Factory res queue uri (local) :{queueConnectionString}/{responseQueueName} "); // rhz:
                    return new QueueClient(queueUri, new StorageSharedKeyCredential(AzuriteAccountName, AzuriteKey));

                default:
                    Console.WriteLine($"Factory res queue params (default) :{queueConnectionString}, {responseQueueName} "); // rhz:
                    return new QueueClient(queueConnectionString, responseQueueName);
            }
        }
    }
}
