using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Monitors
{
    internal class S57BuildResponseMonitor : QueueMonitor<BuildResponse>
    {
        private readonly CompletionPipelineFactory _pipelineFactory;

        public S57BuildResponseMonitor(CompletionPipelineFactory pipelineFactory, QueueServiceClient queueServiceClient, IConfiguration configuration, ILogger<S57BuildResponseMonitor> logger)
            : base(StorageConfiguration.S57BuildResponseQueueName, "Queues:S57ResponseQueue:PollingIntervalSeconds", "Queues:S57ResponseQueue:BatchSize", queueServiceClient, configuration, logger) =>
            _pipelineFactory = pipelineFactory;

        protected override async Task ProcessMessageAsync(BuildResponse messageInstance, CancellationToken stoppingToken)
        {
            var pipelineContext = CompletionPipelineContext.CreateFrom(messageInstance, ExchangeSetDataStandard.S57);

            var pipeline = _pipelineFactory.CreateCompletionPipeline(pipelineContext);
            await pipeline.RunAsync(stoppingToken);
        }
    }
}
