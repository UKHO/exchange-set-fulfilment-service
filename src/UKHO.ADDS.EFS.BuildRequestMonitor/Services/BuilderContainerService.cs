using System.Runtime.InteropServices;
using Docker.DotNet;
using Docker.DotNet.Models;
using Serilog;
using UKHO.ADDS.EFS.BuildRequestMonitor.Builders;
using UKHO.ADDS.EFS.BuildRequestMonitor.Logging;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Services
{
    internal class BuilderContainerService
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<BuilderContainerService> _logger;
        private readonly DockerClient _dockerClient;
        private string _networkName = "efs_test_bridge";
        private string _dynamicContainerName;

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

            await CreateCustomNetworkBridgeAsync(_networkName);
            await AttachContainerToNetworkAsync(containerName: StorageConfiguration.StorageName, networkName: _networkName);

            var containerParams = new CreateContainerParameters
            {
                Image = image,
                Name = name + Guid.NewGuid().ToString("N"),
                Cmd = command,
                AttachStdout = true,
                AttachStderr = true,
                Tty = false,
                HostConfig = new HostConfig
                {
                    NetworkMode = _networkName, //rhz: Use the custom bridge network
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
            };

            var response = await _dockerClient.Containers.CreateContainerAsync(containerParams);
            _dynamicContainerName = containerParams.Name;
            return response.ID;
        }

        public async Task StartContainerAsync(string containerId)
        {
            var started = await _dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
            if (!started)
            {
                throw new Exception("Failed to start container");
            }

            await AttachContainerToNetworkAsync(containerName: _dynamicContainerName, networkName: _networkName);

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


        private async Task<bool> ContainerExistsAsync(string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
            {
                return false;
            }

            var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["name"] = new Dictionary<string, bool> { [containerName] = true }
                }
            });

            return containers.Any(c =>
                c.Names.Any(n => string.Equals(n.TrimStart('/'), containerName, StringComparison.OrdinalIgnoreCase)));
        }

        private async Task CreateCustomNetworkBridgeAsync(string name)
        {
            if (!IsRunningInPipeline())
            {
                Log.Information("Not running in a pipeline; skipping custom network creation.");
                _networkName = string.Empty;
                return;
            }

            var networkParams = new NetworksCreateParameters
            {
                Name = name,
                Driver = "bridge"
            };

            var existing = await _dockerClient.Networks.ListNetworksAsync(new NetworksListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["name"] = new Dictionary<string, bool> { [networkParams.Name] = true }
                }
            });

            var networkExists = existing.Any();
            if (!networkExists)
            {
                try
                {
                    await _dockerClient.Networks.CreateNetworkAsync(networkParams);
                    Log.Information("Created docker custom network {NetworkName}.", networkParams.Name);
                }
                catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    Log.Information("Network {NetworkName} already created by another process.", networkParams.Name);
                }
            }
            else
            {
                Log.Information("Docker custom network {NetworkName} already exists.", networkParams.Name);
            }
        }

        private async Task AttachContainerToNetworkAsync(string containerName, string networkName)
        {
            if (!IsRunningInPipeline())
            {
                Log.Information("Not running in a pipeline; skipping custom network creation.");
                return;
            }

            if (await ContainerExistsAsync(containerName))
            {
                try
                {
                    await _dockerClient.Networks.ConnectNetworkAsync(networkName, new NetworkConnectParameters
                    {
                        Container = containerName
                    });
                    Log.Information("Connected container {Container} to network {Network}.", containerName, networkName);
                }
                catch (AggregateException ae) when (ae.InnerExceptions.Any(e => e is DockerApiException dex && (dex.StatusCode == System.Net.HttpStatusCode.NotModified || dex.StatusCode == System.Net.HttpStatusCode.Conflict)))
                {
                    Log.Information("Container {Container} already connected to network {Network}.", containerName, networkName);
                }
                catch (Exception ex)
                {
                    Log.Information("Some other Container {Container} already connected to network {Network} {message}.", containerName, networkName, ex.Message);
                }
            }
            else
            {
                Log.Warning("Container {Container} not found; skipping network connection.", containerName);
            }
        }

        private bool IsRunningInPipeline()
        {
            // Common environment variables for CI/CD pipelines
            var ci = Environment.GetEnvironmentVariable("CI");
            var tfBuild = Environment.GetEnvironmentVariable("TF_BUILD");
            var githubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
            var azurePipeline = Environment.GetEnvironmentVariable("AGENT_NAME");

            return !string.IsNullOrEmpty(ci)
                || !string.IsNullOrEmpty(tfBuild)
                || !string.IsNullOrEmpty(githubActions)
                || !string.IsNullOrEmpty(azurePipeline);

        }
    }
}
