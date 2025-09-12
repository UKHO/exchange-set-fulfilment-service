using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Domain.Services.Storage;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;

namespace UKHO.ADDS.EFS.Orchestrator.Monitors
{
    internal class S63BuildResponseMonitor : QueueMonitor<BuildResponse>
    {
        private readonly ICompletionPipelineFactory _pipelineFactory;

        public S63BuildResponseMonitor(ICompletionPipelineFactory pipelineFactory, IQueueFactory queueFactory, IConfiguration configuration, ILogger<S57BuildResponseMonitor> logger)
            : base(StorageConfiguration.S63BuildResponseQueueName, "orchestrator:Builders:S63:Responses:PollingIntervalSeconds", "orchestrator:Builders:S63:Responses:BatchSize", queueFactory, configuration, logger) =>
            _pipelineFactory = pipelineFactory;

        protected override async Task ProcessMessageAsync(BuildResponse messageInstance, CancellationToken stoppingToken)
        {
            var pipelineContext = CompletionPipelineParameters.CreateFrom(messageInstance, DataStandard.S63);

            var pipeline = _pipelineFactory.CreateCompletionPipeline(pipelineContext);
            await pipeline.RunAsync(stoppingToken);
        }
    }
}
