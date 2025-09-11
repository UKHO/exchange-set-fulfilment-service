using Serilog;
using System.Runtime.InteropServices;
using Docker.DotNet;
using Docker.DotNet.Models;
using UKHO.ADDS.EFS.BuildRequestMonitor.Builders;
using UKHO.ADDS.EFS.BuildRequestMonitor.Logging;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;
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

            //rhz: Ensure the custom bridge network exists and connect the storage container to it
            //var cntnrs = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters
            //{
            //    All = true,
                
                
            //});

            //foreach (var cntnr in cntnrs)
            //{
            //    if (cntnr.Names.Any(n => n.TrimStart('/').Equals(StorageConfiguration.StorageName, StringComparison.OrdinalIgnoreCase)))
            //    {
            //        Log.Information($"Found existing container with name {StorageConfiguration.StorageName} and ID {cntnr.ID}. Continuing.");
            //    }
            //}

            var networkParams = new NetworksCreateParameters
            {
                Name = "efs_test_bridge",
                Driver = "bridge"
            };

            Log.Information($"Attempt to create docker custom network {networkParams.Name}. ");

            //var networks = await _dockerClient.Networks.ListNetworksAsync();


            //var existingNetwork = networks.FirstOrDefault(n => n.Name == "efs_test_bridge") != null;


            try
            {
                await _dockerClient.Networks.CreateNetworkAsync(networkParams);
                Log.Information($"Created docker custom network {networkParams.Name}. Continuing.");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to create docker network {NetworkName}. Continuing.", networkParams.Name);

            }

            //if (!existingNetwork)
            //{
            //    try
            //    {
            //        await _dockerClient.Networks.CreateNetworkAsync(networkParams);
            //        Log.Information($"Created docker custome network {networkParams.Name}. Continuing.");
            //    }
            //    catch (Exception ex)
            //    {
            //        Log.Warning(ex, "Failed to create docker network {NetworkName}. Continuing.", networkParams.Name);

            //    } 
            //}
            //else
            //{
            //    Log.Information($"Docker custome network {networks.FirstOrDefault(n => n.Name == "efs_test_bridge")} already exists. Continuing.");
            //}


            await _dockerClient.Networks.ConnectNetworkAsync("efs_test_bridge", new NetworkConnectParameters
                {
                    Container = StorageConfiguration.StorageName,
                });
            Log.Information($"Attempted to connect docker custome network {networkParams.Name} to {StorageConfiguration.StorageName}.");



            //rhz: End custom bridge network setup

            var response = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = image,
                Name = name + Guid.NewGuid().ToString("N"),
                Cmd = command,
                AttachStdout = true,
                AttachStderr = true,
                Tty = false,
                HostConfig = new HostConfig
                {
                    NetworkMode = "efs_test_bridge", //rhz: Use the custom bridge network
                    ExtraHosts = new[]
                    {
                        "host.docker.internal:host-gateway"
                    }
                },
                Env = new List<string>
                {
                    $"{BuilderEnvironmentVariables.RequestQueueName}={environment.RequestQueueName}",
                    $"{BuilderEnvironmentVariables.ResponseQueueName}={environment.ResponseQueueName}",
                    $"{BuilderEnvironmentVariables.QueueConnectionString}={environment.QueueConnectionString}",
                    $"{BuilderEnvironmentVariables.BlobConnectionString}={environment.BlobConnectionString}",
                    $"{BuilderEnvironmentVariables.BlobContainerName}={environment.BlobContainerName}",
                    $"{BuilderEnvironmentVariables.FileShareEndpoint}={environment.FileShareEndpoint}",
                    $"{BuilderEnvironmentVariables.FileShareHealthEndpoint}={environment.FileShareHealthEndpoint}",
                    $"{BuilderEnvironmentVariables.FileShareClientId}={string.Empty}",
                    $"{BuilderEnvironmentVariables.AddsEnvironment}={environment.AddsEnvironment}",
                    $"{BuilderEnvironmentVariables.MaxRetryAttempts}={environment.MaxRetryAttempts}",
                    $"{BuilderEnvironmentVariables.RetryDelayMilliseconds}={environment.RetryDelayMilliseconds}",
                    $"{BuilderEnvironmentVariables.ConcurrentDownloadLimitCount}={environment.ConcurrentDownloadLimitCount}",
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
