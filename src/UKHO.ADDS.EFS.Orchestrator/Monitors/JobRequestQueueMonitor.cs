using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.Monitors
{
    internal class JobRequestQueueMonitor : QueueMonitor<JobRequestQueueMessage>
    {
        private readonly AssemblyPipelineFactory _pipelineFactory;

        public JobRequestQueueMonitor(AssemblyPipelineFactory pipelineFactory, QueueServiceClient queueServiceClient, IConfiguration configuration, ILogger<JobRequestQueueMonitor> logger)
            : base(StorageConfiguration.JobRequestQueueName, "Queues:JobRequestQueue:PollingIntervalSeconds", "Queues:JobRequestQueue:BatchSize", queueServiceClient, configuration, logger) =>
            _pipelineFactory = pipelineFactory;

        protected override async Task ProcessMessageAsync(JobRequestQueueMessage messageInstance, CancellationToken stoppingToken)
        {
            var parameters = AssemblyPipelineParameters.CreateFrom(messageInstance, Configuration);
            var pipeline = _pipelineFactory.CreateAssemblyPipeline(parameters);

            var response = await pipeline.RunAsync(stoppingToken);
        }
    }
}
