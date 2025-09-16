﻿using Azure.Storage;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.Infrastructure.Builders.Factories
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

            switch (environment)
            {
                case "local":
                    // We need to construct the client using the "URL" method for running locally so that the container network can connect to Azurite using host.docker.internal
                    // rhz: var queueUri = new Uri($"{queueConnectionString}/{requestQueueName}");
                    // rhz: return new QueueClient(queueUri, new StorageSharedKeyCredential(AzuriteAccountName, AzuriteKey));
                    return new QueueClient(queueConnectionString, requestQueueName);
                default:
                    return new QueueClient(queueConnectionString, requestQueueName);
            }
        }

        public QueueClient CreateResponseQueueClient(IConfiguration configuration)
        {
            var environment = Environment.GetEnvironmentVariable(BuilderEnvironmentVariables.AddsEnvironment)!;
            var queueConnectionString = configuration[BuilderEnvironmentVariables.QueueConnectionString]!;
            var responseQueueName = configuration[BuilderEnvironmentVariables.ResponseQueueName]!;

            switch (environment)
            {
                case "local":
                    // We need to construct the client using the "URL" method for running locally so that the container network can connect to Azurite using host.docker.internal
                    var queueUri = new Uri($"{queueConnectionString}/{responseQueueName}");
                    return new QueueClient(queueUri, new StorageSharedKeyCredential(AzuriteAccountName, AzuriteKey));

                default:
                    return new QueueClient(queueConnectionString, responseQueueName);
            }
        }
    }
}
