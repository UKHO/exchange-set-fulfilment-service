﻿using UKHO.ADDS.Aspire.Configuration;
using UKHO.ADDS.Aspire.Configuration.Remote;
using UKHO.ADDS.EFS.BuildRequestMonitor.Services;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Builders
{
    internal class S100BuildRequestProcessor : BuildRequestMonitor
    {
        private readonly string[] _command = ["sh", "--add-host=host.docker.internal:host-gateway ...","-c", "echo Starting; sleep 5; echo Healthy now; sleep 5; echo Exiting..."];

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
                env.QueueConnectionString = $"http://host.docker.internal:{queuePort}/devstoreaccount1"; 
                env.BlobConnectionString = $"http://host.docker.internal:{blobPort}/devstoreaccount1";
                env.FileShareEndpoint = s100FileShareUri!.ToString();
                env.FileShareHealthEndpoint = s100FileShareHealthUri!.ToString();
                env.BlobContainerName = StorageConfiguration.S100BuildContainer;
                env.MaxRetryAttempts = int.Parse(_configuration["buildRequestMonitor:S100:MaxRetries"]!); 
                env.RetryDelayMilliseconds = int.Parse(_configuration["buildRequestMonitor:S100:RetryDelayMilliseconds"]!);
                env.ConcurrentDownloadLimitCount = int.Parse(_configuration["buildRequestMonitor:S100:ConcurrentDownloadLimitCount"]!);
            });

            Console.WriteLine($"Container {containerId} about to start for S100 build request {request.JobId}.");  //rhz:
            Console.WriteLine($"Container Env AddsEnvironment:{AddsEnvironment.Local.Value} ");  //rhz:
            Console.WriteLine($"Container Env RequestQueueName:{StorageConfiguration.S100BuildRequestQueueName} ");  //rhz:
            Console.WriteLine($"Container Env ResponseQueueName:{StorageConfiguration.S100BuildResponseQueueName} ");  //rhz:
            Console.WriteLine($"Container Env QueueConnectionString:http://host.docker.internal:{queuePort}/devstoreaccount1 ");  //rhz:
            Console.WriteLine($"Container Env BlobConnectionString:http://host.docker.internal:{blobPort}/devstoreaccount1 ");  //rhz:
            Console.WriteLine($"Container Env FileShareEndpoint:{s100FileShareUri!.ToString()} ");  //rhz:
            Console.WriteLine($"Container Env FileShareHealthEndpoint:{s100FileShareHealthUri!.ToString()} ");  //rhz:
            Console.WriteLine($"Container Env BlobContainerName:{StorageConfiguration.S100BuildContainer} ");  //rhz:
            Console.WriteLine($"Container Env MaxRetryAttempts:{_configuration["buildRequestMonitor:S100:MaxRetries"]} ");  //rhz:
            Console.WriteLine($"Container Env RetryDelayMilliseconds:{_configuration["buildRequestMonitor:S100:RetryDelayMilliseconds"]} ");  //rhz:
            Console.WriteLine($"Container Env ConcurrentDownloadLimitCount:{_configuration["buildRequestMonitor:S100:ConcurrentDownloadLimitCount"]} ");  //rhz:
            await _containerService.StartContainerAsync(containerId);
            Console.WriteLine($"Container {containerId} started for S100 build request {request.JobId}.");  //rhz:
        }

    }
}
