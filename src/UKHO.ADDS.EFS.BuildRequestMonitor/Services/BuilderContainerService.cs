using System.Runtime.InteropServices;
using Docker.DotNet;
using Docker.DotNet.Models;
using UKHO.ADDS.EFS.BuildRequestMonitor.Builders;
using UKHO.ADDS.EFS.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Services
{
    internal class BuilderContainerService
    {
        private readonly ILogger<BuilderContainerService> _logger;
        private readonly DockerClient _dockerClient;

        public BuilderContainerService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<BuilderContainerService>();
            _dockerClient = new DockerClientConfiguration(GetDockerEndpoint()).CreateClient();
        }

        public async Task<string> CreateContainerAsync(string image, string name, string[] command, string id, string batchId, Action<BuilderEnvironment> setEnvironmentFunc)
        {
            var environment = new BuilderEnvironment();
            setEnvironmentFunc?.Invoke(environment);

            var response = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = image,
                Name = name,
                Cmd = command,
                AttachStdout = true,
                AttachStderr = true,
                Tty = false,
                Env = new List<string>
                {
                    $"{BuilderEnvironmentVariables.RequestQueueName}={environment.RequestQueueName}",
                    $"{BuilderEnvironmentVariables.ResponseQueueName}={environment.ResponseQueueName}",
                    $"{BuilderEnvironmentVariables.QueueConnectionString}={environment.QueueConnectionString}",
                    $"{BuilderEnvironmentVariables.BlobConnectionString}={environment.BlobConnectionString}",
                    $"{BuilderEnvironmentVariables.BlobContainerName}={environment.BlobContainerName}",
                    $"{BuilderEnvironmentVariables.AddsEnvironment}={environment.AddsEnvironment}"
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
            var started = await _dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
            if (!started)
            {
                throw new Exception("Failed to start container");
            }
        }

        public async Task StopContainerAsync(string containerId, CancellationToken stoppingToken) => await _dockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters(), stoppingToken);

        private static Uri GetDockerEndpoint() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new Uri("npipe://./pipe/docker_engine")
            : new Uri("unix:///var/run/docker.sock");
    }
}
