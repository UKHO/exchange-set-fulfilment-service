using System.Runtime.InteropServices;
using Docker.DotNet;
using Docker.DotNet.Models;
using UKHO.ADDS.EFS.BuildRequestMonitor.Builders;
using UKHO.ADDS.EFS.BuildRequestMonitor.Logging;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Services
{
    internal class BuilderContainerService
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<BuilderContainerService> _logger;
        private readonly DockerClient _dockerClient;

        public BuilderContainerService(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<BuilderContainerService>();
            _dockerClient = new DockerClientConfiguration(GetDockerEndpoint()).CreateClient();
        }

        public async Task<string> CreateContainerAsync(string image, string name, string[] command, BuildRequest request, Action<BuilderEnvironment> setEnvironmentFunc)
        {
            var environment = new BuilderEnvironment();
            setEnvironmentFunc?.Invoke(environment);

            var response = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = image,
                Name = name + Guid.NewGuid().ToString("N"),
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
                    $"{BuilderEnvironmentVariables.FileShareEndpoint}={environment.FileShareEndpoint}",
                    $"{BuilderEnvironmentVariables.FileShareHealthEndpoint}={environment.FileShareHealthEndpoint}",
                    $"{BuilderEnvironmentVariables.AddsEnvironment}={environment.AddsEnvironment}",
                    $"{BuilderEnvironmentVariables.MaxRetryAttempts}={environment.MaxRetryAttempts}",
                    $"{BuilderEnvironmentVariables.RetryDelayMilliseconds}={environment.RetryDelayMilliseconds}",
                }
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

            var streamer = new BuilderLogStreamer(_dockerClient);

            var forwarder = new LogForwarder(_loggerFactory.CreateLogger(containerId), containerId);

            var logTask = streamer.StreamLogsAsync(
                containerId,
#pragma warning disable LOG001
                line =>
                {
                    forwarder.ForwardLog(LogLevel.Information, line);
                },
                line =>
                {
                    forwarder.ForwardLog(LogLevel.Error, line);
                },
#pragma warning restore LOG001
                CancellationToken.None
            );
        }

        public async Task StopContainerAsync(string containerId, CancellationToken stoppingToken) => await _dockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters(), stoppingToken);

        private static Uri GetDockerEndpoint() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new Uri("npipe://./pipe/docker_engine")
            : new Uri("unix:///var/run/docker.sock");
    }
}
