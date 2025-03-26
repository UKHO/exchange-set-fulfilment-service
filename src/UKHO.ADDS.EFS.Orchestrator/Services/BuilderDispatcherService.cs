using System.Threading.Channels;
using Azure.Data.Tables;
using Serilog;
using UKHO.ADDS.EFS.Common.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Common.Entities;
using UKHO.ADDS.EFS.Common.Messages;
using UKHO.ADDS.EFS.Orchestrator.Tables;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    internal class BuilderDispatcherService : BackgroundService
    {
        private const string ImageName = "efs-builder-s100";
        private const string ContainerName = "efs-builder-s100-";
        private readonly BuilderStartup _builderStartup;

        private readonly Channel<ExchangeSetRequestMessage> _channel;
        private readonly ExchangeSetRequestTable _exchangeSetRequestTable;

        private readonly string[] _command = { "sh", "-c", "echo Starting; sleep 5; echo Healthy now; sleep 5; echo Exiting..." };

        private readonly SemaphoreSlim _concurrencyLimiter;
        private readonly ContainerService _containerService;

        private readonly TimeSpan _containerTimeout = TimeSpan.FromSeconds(20);

        public BuilderDispatcherService(Channel<ExchangeSetRequestMessage> channel, ExchangeSetRequestTable exchangeSetRequestTable, IConfiguration configuration)
        {
            _channel = channel;
            _exchangeSetRequestTable = exchangeSetRequestTable;

            var builderStartupValue = Environment.GetEnvironmentVariable(OrchestratorEnvironmentVariables.BuilderStartup);
            if (builderStartupValue == null)
            {
                throw new InvalidOperationException($"Environment variable {OrchestratorEnvironmentVariables.BuilderStartup} is not set");
            }

            _builderStartup = Enum.Parse<BuilderStartup>(builderStartupValue);

            var fileShareEndpoint = Environment.GetEnvironmentVariable(OrchestratorEnvironmentVariables.FileShareEndpoint)!;
            var salesCatalogueEndpoint = Environment.GetEnvironmentVariable(OrchestratorEnvironmentVariables.SalesCatalogueEndpoint)!;

            var builderServiceEndpoint = Environment.GetEnvironmentVariable(OrchestratorEnvironmentVariables.BuildServiceEndpoint)!;
            var builderServiceContainerEndpoint = new UriBuilder(builderServiceEndpoint) { Host = "host.docker.internal" }.ToString();

            var maxConcurrentBuilders = configuration.GetValue<int>("Builders:MaximumConcurrentBuilders");
            _concurrencyLimiter = new SemaphoreSlim(maxConcurrentBuilders, maxConcurrentBuilders);

            _containerService = new ContainerService(fileShareEndpoint, salesCatalogueEndpoint, builderServiceContainerEndpoint);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                await _concurrencyLimiter.WaitAsync(stoppingToken);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var id = Guid.NewGuid().ToString("N");

                        switch (_builderStartup)
                        {
                            case BuilderStartup.Manual:
                                await StoreRequest(WellKnownRequestId.DebugRequestId, request);
                                break;

                            case BuilderStartup.Orchestrator:
                                await StoreRequest(id, request);
                                await ExecuteBuilder(request, id, stoppingToken);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to process message");
                    }

                    finally
                    {
                        _concurrencyLimiter.Release();
                    }
                }, stoppingToken);
            }
        }

        private async Task ExecuteBuilder(ExchangeSetRequestMessage request, string requestId, CancellationToken stoppingToken)
        {
            var containerName = $"{ContainerName}{requestId}";

            await _containerService.EnsureImageExistsAsync(ImageName);

            var containerId = await _containerService.CreateContainerAsync(ImageName, containerName, _command, requestId);
            await _containerService.StartContainerAsync(containerId);

            var streamer = _containerService.CreateBuilderLogStreamer();

            var logTask = streamer.StreamLogsAsync(
                containerId,
                line =>
                {
                    Log.Information($"[{containerName}] {line.ReplaceLineEndings("")}");
                },
                line => { Log.Error($"[{containerName}] {line}"); },
                stoppingToken
            );

            try
            {
                var exitCode = await _containerService.WaitForContainerExitAsync(containerId, _containerTimeout);
                Log.Information($"Container {containerId} exited with code: {exitCode}");
            }
            catch (TimeoutException)
            {
                Log.Error($"Container {containerId} exceeded timeout. Killing...");
                await _containerService.StopContainerAsync(containerId, stoppingToken);
            }

            await logTask;

            await _containerService.RemoveContainerAsync(containerId, stoppingToken);
        }

        private async Task StoreRequest(string requestId, ExchangeSetRequestMessage request)
        {
            var requestEntity = new ExchangeSetRequest { Id = requestId, Message = request };

            await _exchangeSetRequestTable.CreateTableIfNotExistsAsync();
            await _exchangeSetRequestTable.AddAsync(requestEntity);

            var entities = await _exchangeSetRequestTable.ListAsync();
            var entity = await _exchangeSetRequestTable.GetAsync(requestId, requestId);

            var entity2 = await _exchangeSetRequestTable.GetAsync(requestId);
        }
    }
}
