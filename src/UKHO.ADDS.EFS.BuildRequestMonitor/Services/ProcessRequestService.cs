using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Services;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Services
{
    public class ProcessRequestService
    {
        private const string ImageName = "efs-builder-s100";
        private const string ContainerName = "efs-builder-s100-";
        private readonly BuilderContainerService _containerService;
        private readonly string[] _command = ["sh", "-c", "echo Starting; sleep 5; echo Healthy now; sleep 5; echo Exiting..."];
        //private readonly string _workspaceAuthenticationKey;
        //private readonly string _fileShareBuilderEndpoint;

        public ProcessRequestService(IConfiguration config, BuilderContainerService bcs)
        {
            _containerService = bcs ?? throw new ArgumentNullException(nameof(bcs), "BuilderContainerService cannot be null");
        }
        // This service will handle the processing of requests
        // It will monitor the request queue and process each request accordingly
        public async Task ProcessRequestAsync(string request, CancellationToken cancellationToken)
        {
            var data = JsonCodec.Decode<BuilderRequestQueueMessage>(request);
            var containerName = $"{ContainerName}{data.JobId}";

            await _containerService.EnsureImageExistsAsync(ImageName);
            var containerId = await _containerService.CreateContainerAsync(ImageName, containerName, _command, data.JobId, data.BatchId);
            await _containerService.StartContainerAsync(containerId);
        }
    }
}
