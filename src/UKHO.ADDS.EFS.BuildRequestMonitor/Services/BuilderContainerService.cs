using System.Runtime.InteropServices;
using Docker.DotNet;
using Docker.DotNet.Models;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Services
{
    internal class BuilderContainerService
    {
        private readonly string _fileShareBuilderEndpoint;
        private readonly ILogger<BuilderContainerService> _logger;
        private readonly string _workspaceAuthenticationKey;

        public BuilderContainerService(ILoggerFactory loggerFactory, IConfiguration config)
        {
            _logger = loggerFactory.CreateLogger<BuilderContainerService>();
            var mockEndpoint = config[$"services:{ProcessNames.MockService}:http:0"] ?? string.Empty;
            var fssBuilderEndpoint = new UriBuilder(mockEndpoint) { Host = "host.docker.internal", Path = "fss/" };
            _fileShareBuilderEndpoint = fssBuilderEndpoint.Uri.ToString();

            _workspaceAuthenticationKey = config["WorkspaceKey"] ?? string.Empty;

            var test = new BuilderRequestQueueMessage { BatchId = "test", JobId = "test", StorageAddress = "test", CorrelationId = "12345671898" };
            var data = JsonCodec.Encode(test);

            DockerClient = new DockerClientConfiguration(GetDockerEndpoint()).CreateClient();
        }

        internal DockerClient DockerClient { get; }

        public async Task<string> CreateContainerAsync(string image, string name, string[] command, string id, string batchId)
        {
            var response = await DockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = image,
                Name = name,
                Cmd = command,
                AttachStdout = true,
                AttachStderr = true,
                Tty = false,
                Env = new List<string>
                {
                    $"{BuilderEnvironmentVariables.JobId}={id}",
                    $"{BuilderEnvironmentVariables.FileShareEndpoint}={_fileShareBuilderEndpoint}",
                    //$"{BuilderEnvironmentVariables.BuildServiceEndpoint}={_builderServiceContainerEndpoint}",
                    //$"{BuilderEnvironmentVariables.OtlpEndpoint}={_otlpContainerEndpoint}",
                    $"{BuilderEnvironmentVariables.WorkspaceKey}={_workspaceAuthenticationKey}",
                    $"{BuilderEnvironmentVariables.BatchId}={batchId}"
                },
                Healthcheck = new HealthConfig
                {
                    Test = new[] { "CMD-SHELL", "echo healthy" },
                    Interval = TimeSpan.FromSeconds(3),
                    Timeout = TimeSpan.FromSeconds(2),
                    Retries = 3,
                    StartPeriod = (long)TimeSpan.FromSeconds(2).TotalMilliseconds * 1000000
                },
                HostConfig = new HostConfig { RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.OnFailure, MaximumRetryCount = 3 } }
            });

            return response.ID;
        }

        public async Task StartContainerAsync(string containerId)
        {
            var started = await DockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
            if (!started)
            {
                throw new Exception("Failed to start container");
            }
        }

        public async Task StopContainerAsync(string containerId, CancellationToken stoppingToken) => await DockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters(), stoppingToken);

        private static Uri GetDockerEndpoint() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new Uri("npipe://./pipe/docker_engine")
            : new Uri("unix:///var/run/docker.sock");
    }
}
