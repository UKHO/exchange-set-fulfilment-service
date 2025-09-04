using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Domain.Services.Storage;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;

namespace UKHO.ADDS.EFS.Orchestrator.Monitors
{
    internal class S57BuildResponseMonitor : QueueMonitor<BuildResponse>
    {
        private readonly ICompletionPipelineFactory _pipelineFactory;

        public S57BuildResponseMonitor(ICompletionPipelineFactory pipelineFactory, IQueueFactory queueFactory, IConfiguration configuration, ILogger<S57BuildResponseMonitor> logger)
            : base(StorageConfiguration.S57BuildResponseQueueName, "orchestrator:Builders:S57:Responses:PollingIntervalSeconds", "orchestrator:Builders:S57:Responses:BatchSize", queueFactory, configuration, logger) =>
            _pipelineFactory = pipelineFactory;

        protected override async Task ProcessMessageAsync(BuildResponse messageInstance, CancellationToken stoppingToken)
        {
            var pipelineContext = CompletionPipelineParameters.CreateFrom(messageInstance, DataStandard.S57);

            var pipeline = _pipelineFactory.CreateCompletionPipeline(pipelineContext);
            await pipeline.RunAsync(stoppingToken);
        }
    }
}
