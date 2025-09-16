﻿using UKHO.ADDS.Aspire.Configuration;
using UKHO.ADDS.Aspire.Configuration.Remote;
using UKHO.ADDS.EFS.BuildRequestMonitor.Services;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Builders
{
    internal class S100BuildRequestProcessor : BuildRequestMonitor
    {
        private readonly string[] _command = ["sh", "-c", "echo Starting; sleep 5; echo Healthy now; sleep 5; echo Exiting..."];

        private readonly BuilderContainerService _containerService;
        private readonly IConfiguration _configuration;
        private readonly IExternalServiceRegistry _externalServiceRegistry;

        public S100BuildRequestProcessor(BuilderContainerService bcs, IConfiguration configuration, IExternalServiceRegistry externalServiceRegistry)
        {
            _containerService = bcs;
            _configuration = configuration;
            _externalServiceRegistry = externalServiceRegistry;
        }

        /// <summary>
        ///     Sets the container environment and starts the container for processing the S100 build request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task ProcessRequestAsync(S100BuildRequest request, CancellationToken cancellationToken)
        {
            var containerName = $"{ProcessNames.S100Builder}-{request.JobId}";

            var queueConnectionString = _configuration[$"ConnectionStrings:{StorageConfiguration.QueuesName}"]!;
            var blobConnectionString = _configuration[$"ConnectionStrings:{StorageConfiguration.BlobsName}"]!;
            
            var queuePort = ExtractPort(queueConnectionString, "QueueEndpoint");
            var blobPort = ExtractPort(blobConnectionString, "BlobEndpoint");

            var s100FileShareEndpoint = _externalServiceRegistry.GetServiceEndpoint(ProcessNames.FileShareService, "", EndpointHostSubstitution.Docker);
            var s100FileShareUri = s100FileShareEndpoint.Uri;

            var s100FileShareHealthUri = new Uri(s100FileShareUri!, "health");

            // Set the environment variables for the container - in production, these are set from the Azure environment (via the pipeline)
            var containerId = await _containerService.CreateContainerAsync(ProcessNames.S100Builder, containerName, _command, request, env =>
            {
                env.AddsEnvironment = AddsEnvironment.Local.Value;
                env.RequestQueueName = StorageConfiguration.S100BuildRequestQueueName;
                env.ResponseQueueName = StorageConfiguration.S100BuildResponseQueueName;
                //env.QueueConnectionString = $"http://host.docker.internal:{queuePort}/devstoreaccount1";
                env.QueueConnectionString = $"http://{StorageConfiguration.StorageName:10001}/devstoreaccount1"; //rhz
                //env.BlobConnectionString = $"http://host.docker.internal:{blobPort}/devstoreaccount1";
                env.BlobConnectionString = $"http://{StorageConfiguration.StorageName}:10000/devstoreaccount1"; //rhz

                env.FileShareEndpoint = s100FileShareUri!.ToString();
                env.FileShareHealthEndpoint = s100FileShareHealthUri!.ToString();
                env.BlobContainerName = StorageConfiguration.S100BuildContainer;
                env.MaxRetryAttempts = int.Parse(_configuration["buildRequestMonitor:S100:MaxRetries"]!); 
                env.RetryDelayMilliseconds = int.Parse(_configuration["buildRequestMonitor:S100:RetryDelayMilliseconds"]!);
                env.ConcurrentDownloadLimitCount = int.Parse(_configuration["buildRequestMonitor:S100:ConcurrentDownloadLimitCount"]!);
            });

            await _containerService.StartContainerAsync(containerId);
        }

    }
}
