using System.Threading.Channels;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Builders.S100;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Tables;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Monitors.S100
{
    internal class S100BuildResponseMonitor : BackgroundService
    {
        private readonly S100BuildResponseProcessor _buildResponseService;
        private readonly BuildSummaryTable _buildSummaryTable;
        private readonly ILogger<S100BuildResponseMonitor> _logger;
        private readonly QueueClient _queueClient;
        private readonly int _pollingIntervalSeconds;
        private readonly int _queueBatchSize;

        public S100BuildResponseMonitor(S100BuildResponseProcessor buildResponseService, QueueServiceClient queueServiceClient, BuildSummaryTable buildSummaryTable, IConfiguration configuration, ILogger<S100BuildResponseMonitor> logger)
        {
            _buildResponseService = buildResponseService;
            _buildSummaryTable = buildSummaryTable;
            _logger = logger;

            _queueClient = queueServiceClient.GetQueueClient(StorageConfiguration.S100BuildResponseQueueName);

            _pollingIntervalSeconds = configuration.GetValue<int>("S100ResponseQueue:PollingIntervalSeconds");
            _queueBatchSize = configuration.GetValue<int>("S100ResponseQueue:BatchSize");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    QueueMessage[] queueMessages = await _queueClient.ReceiveMessagesAsync(_queueBatchSize, cancellationToken: stoppingToken);

                    foreach (var message in queueMessages)
                    {
                        var buildResponse = JsonCodec.Decode<BuildResponse>(message.MessageText)!;
                        var buildSummaryResult = await _buildSummaryTable.GetAsync(buildResponse.JobId, $"{buildResponse.JobId}-summary");

                        if (buildSummaryResult.IsSuccess(out var buildSummary))
                        {
                            await _buildResponseService.ProcessBuildResponseAsync(buildResponse, buildSummary, stoppingToken);

                            // TODO Error handling etc

                            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                            await _buildSummaryTable.DeleteAsync(buildResponse.JobId, $"{buildResponse.JobId}-summary");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogQueueServiceMessageReadFailed(nameof(S100BuildResponseMonitor), ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(_pollingIntervalSeconds), stoppingToken);
            }
        }
    }
}
