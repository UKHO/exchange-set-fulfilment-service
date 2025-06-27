using System.Threading.Channels;
using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Logging;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    internal class BuilderDispatcherService : BackgroundService
    {
        private const string ImageName = "efs-builder-s100";
        private const string ContainerName = "efs-builder-s100-";

        private readonly ILogger<BuilderDispatcherService> _logger;
        private readonly ILoggerFactory _loggerFactory;

        private readonly QueueServiceClient _requestQueueClient;

        private readonly Channel<ExchangeSetRequestQueueMessage> _channel;
        private readonly JobService _jobService;

        private readonly string[] _command = ["sh", "-c", "echo Starting; sleep 5; echo Healthy now; sleep 5; echo Exiting..."];

        private readonly SemaphoreSlim _concurrencyLimiter;
        private readonly BuilderContainerService _containerService;

        // TODO Figure out how best to control this timeout
        private readonly TimeSpan _containerTimeout = TimeSpan.FromMinutes(5);

        public BuilderDispatcherService(Channel<ExchangeSetRequestQueueMessage> channel, JobService jobService, IConfiguration configuration, ILoggerFactory loggerFactory, QueueServiceClient qClient)
        {
            _channel = channel;
            _jobService = jobService;
            _requestQueueClient = qClient;

            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<BuilderDispatcherService>();

            var builderFileShareEndpoint = configuration["Endpoints:BuilderFileShare"]!;
            var builderOrchestratorEndpoint = configuration["Endpoints:BuilderOrchestrator"]!;
            var workspaceAuthenticationKeySecret = configuration["WorkspaceKey"]!;

            var otlpEndpoint = configuration[GlobalEnvironmentVariables.OtlpEndpoint]!;
            var otlpContainerEndpoint = new UriBuilder(otlpEndpoint) { Host = "host.docker.internal" }.ToString();

            var maxConcurrentBuilders = configuration.GetValue<int>("Builders:MaximumConcurrentBuilders");
            _concurrencyLimiter = new SemaphoreSlim(maxConcurrentBuilders, maxConcurrentBuilders);

            _containerService = new BuilderContainerService(workspaceAuthenticationKeySecret, builderFileShareEndpoint, builderOrchestratorEndpoint, otlpContainerEndpoint, loggerFactory);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var requestQueue = _requestQueueClient.GetQueueClient(StorageConfiguration.S100BuildRequestQueueName);

            await foreach (var queueMessage in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    var job = await _jobService.CreateJob(queueMessage);

                    if (job.State != ExchangeSetJobState.InProgress)
                    {
                        return;
                    }

                    await _concurrencyLimiter.WaitAsync(stoppingToken);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            //await ExecuteBuilder(job, stoppingToken);
                            var builderQueueMessage = new BuilderRequestQueueMessage
                            {
                                JobId = job.Id,
                                StorageAddress = "Unknown",
                                BatchId = job.BatchId,
                                CorrelationId = job.CorrelationId
                            };
                            var buildermessageJson = JsonCodec.Encode(builderQueueMessage);
                            await requestQueue.SendMessageAsync(buildermessageJson, stoppingToken);
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
