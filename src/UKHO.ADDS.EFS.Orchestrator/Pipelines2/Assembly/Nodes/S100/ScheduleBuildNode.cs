using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Assembly.Nodes.S100
{
    internal class ScheduleBuildNode : AssemblyPipelineNode<S100Build>
    {
        private readonly QueueClient _queueClient;


        protected ScheduleBuildNode(NodeEnvironment nodeEnvironment, QueueServiceClient queueServiceClient)
            : base(nodeEnvironment)
        {
            _queueClient = queueServiceClient.GetQueueClient(StorageConfiguration.S100BuildRequestQueueName);
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var jobState = context.Subject.Job.JobState;
            var buildState = context.Subject.Job.BuildState;

            var batchId = context.Subject.Job.BatchId;

            return Task.FromResult((jobState == JobState.Created && buildState == BuildState.NotScheduled) && !string.IsNullOrEmpty(batchId));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;

            var request = new S100BuildRequest
            {
                Version = 1,
                Timestamp = DateTime.UtcNow,
                JobId = job.Id,
                BatchId = job.BatchId!,
                DataStandard = job.DataStandard,
                WorkspaceKey = Environment.Configuration["IICWorkspaceKey"]!,
                ExchangeSetNameTemplate = Environment.Configuration["S100ExchangeSetNameTemplate"]!
            };

            var messageJson = JsonCodec.Encode(request);
            await _queueClient.SendMessageAsync(messageJson, Environment.CancellationToken);

            await context.Subject.BuildScheduled();

            return NodeResultStatus.Succeeded;
        }
    }
}
