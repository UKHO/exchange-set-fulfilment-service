using System.Threading.Channels;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    internal class BuilderDispatcherService : BackgroundService
    {
        private const string ImageName = "efs-builder-s100";
        private const string ContainerName = "efs-builder-s100-";

        private readonly Channel<ExchangeSetRequestMessage> _channel;
        private readonly JobService _jobService;
        private readonly ILogger<BuilderDispatcherService> _logger;
        private readonly string[] _command = ["sh", "-c", "echo Starting; sleep 5; echo Healthy now; sleep 5; echo Exiting..."];

        private readonly SemaphoreSlim _concurrencyLimiter;
        private readonly BuilderContainerService _containerService;

        // TODO Figure out how best to control this timeout
        private readonly TimeSpan _containerTimeout = TimeSpan.FromMinutes(5);

        public BuilderDispatcherService(Channel<ExchangeSetRequestMessage> channel, JobService jobService, IConfiguration configuration, ILogger<BuilderDispatcherService> logger, ILoggerFactory loggerFactory)
        {
            _channel = channel;
            _jobService = jobService;
            _logger = logger;

            var fileShareEndpoint = Environment.GetEnvironmentVariable(OrchestratorEnvironmentVariables.FileShareEndpoint)!;

            var builderServiceEndpoint = Environment.GetEnvironmentVariable(OrchestratorEnvironmentVariables.BuildServiceEndpoint)!;
            var builderServiceContainerEndpoint = new UriBuilder(builderServiceEndpoint) { Host = "host.docker.internal" }.ToString();

            var maxConcurrentBuilders = configuration.GetValue<int>("Builders:MaximumConcurrentBuilders");
            _concurrencyLimiter = new SemaphoreSlim(maxConcurrentBuilders, maxConcurrentBuilders);

            _containerService = new BuilderContainerService(fileShareEndpoint, builderServiceContainerEndpoint, loggerFactory);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    var job = await _jobService.CreateJob(request);

                    if (job.State == ExchangeSetJobState.Cancelled)
                    {
                        _logger.LogInformation("Job {job.Id} was not run as no new products", job.Id);
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
                            _logger.LogError(ex, "Failed to execute builder");
                        }

                        finally
                        {
                            _concurrencyLimiter.Release();
                        }
                    }, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create job");
                    return;
                }
            }
        }

        private async Task ExecuteBuilder(ExchangeSetJob job, CancellationToken stoppingToken)
        {
            var containerName = $"{ContainerName}{job.Id}";

            await _containerService.EnsureImageExistsAsync(ImageName);

            var containerId = await _containerService.CreateContainerAsync(ImageName, containerName, _command, job.Id);
            await _containerService.StartContainerAsync(containerId);

            var streamer = _containerService.CreateBuilderLogStreamer();

            var logTask = streamer.StreamLogsAsync(
                containerId,
                line => { _logger.LogInformation($"[{containerName}] {line.ReplaceLineEndings("")}"); },
                line => { _logger.LogError($"[{containerName}] {line}"); },
                stoppingToken
            );

            try
            {
                var exitCode = await _containerService.WaitForContainerExitAsync(containerId, _containerTimeout);
                _logger.LogInformation("Container {containerId} exited with code: {exitCode}", containerId, exitCode);

                await _jobService.CompleteJobAsync(exitCode, job);
            }

            catch (TimeoutException)
            {
                _logger.LogError("Container {containerId} exceeded timeout. Killing...", containerId);
                await _containerService.StopContainerAsync(containerId, stoppingToken);
            }

            await logTask;

            await _containerService.RemoveContainerAsync(containerId, stoppingToken);
        }
    }
}
