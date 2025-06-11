using System.Threading.Channels;
using Azure.Security.KeyVault.Secrets;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Logging;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    internal class BuilderDispatcherService : BackgroundService
    {
        private const string ImageName = "efs-builder-s100";
        private const string ContainerName = "efs-builder-s100-";

        private readonly ILogger<BuilderDispatcherService> _logger;
        private readonly ILoggerFactory _loggerFactory;

        private readonly Channel<ExchangeSetRequestQueueMessage> _channel;
        private readonly JobService _jobService;
        
        private readonly string[] _command = ["sh", "-c", "echo Starting; sleep 5; echo Healthy now; sleep 5; echo Exiting..."];

        private readonly SemaphoreSlim _concurrencyLimiter;
        private readonly BuilderContainerService _containerService;

        // TODO Figure out how best to control this timeout
        private readonly TimeSpan _containerTimeout = TimeSpan.FromMinutes(5);

        public BuilderDispatcherService(Channel<ExchangeSetRequestQueueMessage> channel, JobService jobService, SecretClient secretClient, IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _channel = channel;
            _jobService = jobService;

            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<BuilderDispatcherService>();

            var fileShareEndpointSecret = secretClient.GetSecret(OrchestratorConfigurationKeys.FileShareEndpoint)!;
            var builderServiceEndpointSecret = secretClient.GetSecret(OrchestratorConfigurationKeys.OrchestratorServiceEndpoint)!;
            var workspaceAuthenticationKeySecret = secretClient.GetSecret(OrchestratorConfigurationKeys.WorkspaceKey)!;

            var builderServiceContainerEndpoint = new UriBuilder(builderServiceEndpointSecret.Value.Value) { Host = "host.docker.internal" }.ToString();

            var otlpEndpoint = configuration[GlobalEnvironmentVariables.OtlpEndpoint]!;
            var otlpContainerEndpoint = new UriBuilder(otlpEndpoint) { Host = "host.docker.internal" }.ToString();

            var maxConcurrentBuilders = configuration.GetValue<int>("Builders:MaximumConcurrentBuilders");
            _concurrencyLimiter = new SemaphoreSlim(maxConcurrentBuilders, maxConcurrentBuilders);

            _containerService = new BuilderContainerService(workspaceAuthenticationKeySecret.Value.Value, fileShareEndpointSecret.Value.Value, builderServiceContainerEndpoint, otlpContainerEndpoint, loggerFactory);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var queueMessage in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    var job = await _jobService.CreateJob(queueMessage);

                    if (job.State is ExchangeSetJobState.Succeeded or ExchangeSetJobState.Cancelled or ExchangeSetJobState.Failed)
                    {
                        return;
                    }

                    await _concurrencyLimiter.WaitAsync(stoppingToken);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ExecuteBuilder(job, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogContainerExecutionFailed(ExchangeSetJobLogView.CreateFromJob(job), ex);
                        }

                        finally
                        {
                            _concurrencyLimiter.Release();
                        }
                    }, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogJobCreationFailed(ex);
                    return;
                }
            }
        }

        private async Task ExecuteBuilder(ExchangeSetJob job, CancellationToken stoppingToken)
        {
            var containerName = $"{ContainerName}{job.Id}";

            await _containerService.EnsureImageExistsAsync(ImageName);

            var containerId = await _containerService.CreateContainerAsync(ImageName, containerName, _command, job.Id, job.BatchId);
            await _containerService.StartContainerAsync(containerId);

            var streamer = _containerService.CreateBuilderLogStreamer();

            var forwarder = new LogForwarder(_loggerFactory.CreateLogger(containerName), job, containerName);

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
                stoppingToken
            );

            try
            {
                var exitCode = await _containerService.WaitForContainerExitAsync(containerId, _containerTimeout);

                await _jobService.BuilderContainerCompletedAsync(exitCode, job);
            }

            catch (TimeoutException)
            {
                _logger.LogContainerTimeout(containerId, ExchangeSetJobLogView.CreateFromJob(job));
                await _containerService.StopContainerAsync(containerId, stoppingToken);
            }

            await logTask;

            await _containerService.RemoveContainerAsync(containerId, stoppingToken);
        }
    }
}
