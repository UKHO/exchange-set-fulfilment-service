using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Services;
using UKHO.ADDS.EFS.VOS;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100
{
    internal class ScheduleBuildNode : AssemblyPipelineNode<S100Build>
    {
        private readonly IStorageService _storageService;
        private readonly QueueClient _queueClient;


        public ScheduleBuildNode(AssemblyNodeEnvironment nodeEnvironment, QueueServiceClient queueServiceClient, IStorageService storageService)
            : base(nodeEnvironment)
        {
            _storageService = storageService;
            _queueClient = queueServiceClient.GetQueueClient(StorageConfiguration.S100BuildRequestQueueName);
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var jobState = context.Subject.Job.JobState;
            var buildState = context.Subject.Job.BuildState;

            var batchId = context.Subject.Job.BatchId;

            return Task.FromResult((jobState == JobState.Created && buildState == BuildState.NotScheduled) && batchId != BatchId.None);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;

            var storeBuildResult = await _storageService.UpdateS100BuildAsync(context.Subject.Build);

            if (storeBuildResult.IsSuccess())
            {
                var request = new S100BuildRequest
                {
                    Timestamp = DateTime.UtcNow,
                    JobId = job.Id,
                    BatchId = job.BatchId!,
                    DataStandard = job.DataStandard,
                    WorkspaceKey = Environment.Configuration["orchestrator:Builders:S100:IICWorkspaceKey"]!,
                    ExchangeSetNameTemplate = Environment.Configuration["orchestrator:Builders:S100:ExchangeSetNameTemplate"]!
                };

                var messageJson = JsonCodec.Encode(request);
                await _queueClient.SendMessageAsync(messageJson, Environment.CancellationToken);

                await context.Subject.SignalBuildScheduled();

                return NodeResultStatus.Succeeded;
            }

            return NodeResultStatus.Failed;
        }
    }
}
