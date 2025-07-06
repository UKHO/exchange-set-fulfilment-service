using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Monitors
{
    internal class S63BuildResponseMonitor : QueueMonitor<BuildResponse>
    {
        private readonly CompletionPipelineFactory _pipelineFactory;

        public S63BuildResponseMonitor(CompletionPipelineFactory pipelineFactory, QueueServiceClient queueServiceClient, IConfiguration configuration, ILogger<S57BuildResponseMonitor> logger)
            : base(StorageConfiguration.S63BuildResponseQueueName, "Queues:S63ResponseQueue:PollingIntervalSeconds", "Queues:S63ResponseQueue:BatchSize", queueServiceClient, configuration, logger) =>
            _pipelineFactory = pipelineFactory;

        protected override async Task ProcessMessageAsync(BuildResponse messageInstance, CancellationToken stoppingToken)
        {
            var pipelineContext = CompletionPipelineContext.CreateFrom(messageInstance, ExchangeSetDataStandard.S63);

            var pipeline = _pipelineFactory.CreateCompletionPipeline(pipelineContext);
            await pipeline.RunAsync(stoppingToken);
        }
    }
}
