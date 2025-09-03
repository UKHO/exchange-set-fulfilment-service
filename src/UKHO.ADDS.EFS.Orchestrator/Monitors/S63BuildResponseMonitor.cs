using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;

namespace UKHO.ADDS.EFS.Orchestrator.Monitors
{
    internal class S63BuildResponseMonitor : QueueMonitor<BuildResponse>
    {
        private readonly CompletionPipelineFactory _pipelineFactory;

        public S63BuildResponseMonitor(CompletionPipelineFactory pipelineFactory, QueueServiceClient queueServiceClient, IConfiguration configuration, ILogger<S57BuildResponseMonitor> logger)
            : base(StorageConfiguration.S63BuildResponseQueueName, "orchestrator:Builders:S63:Responses:PollingIntervalSeconds", "orchestrator:Builders:S63:Responses:BatchSize", queueServiceClient, configuration, logger) =>
            _pipelineFactory = pipelineFactory;

        protected override async Task ProcessMessageAsync(BuildResponse messageInstance, CancellationToken stoppingToken)
        {
            var pipelineContext = CompletionPipelineParameters.CreateFrom(messageInstance, DataStandard.S63);

            var pipeline = _pipelineFactory.CreateCompletionPipeline(pipelineContext);
            await pipeline.RunAsync(stoppingToken);
        }
    }
}
