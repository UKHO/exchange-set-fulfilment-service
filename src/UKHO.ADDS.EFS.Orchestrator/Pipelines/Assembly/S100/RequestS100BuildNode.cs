using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.S100
{
    internal class RequestS100BuildNode : AssemblyPipelineNode<ExchangeSetJob>
    {
        private readonly QueueClient _queueClient;

        public RequestS100BuildNode(NodeEnvironment environment, QueueServiceClient queueServiceClient)
            : base(environment) =>
            _queueClient = queueServiceClient.GetQueueClient(StorageConfiguration.S100BuildRequestQueueName);

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetJob> context)
        {
            var job = context.Subject;

            var request = new BuildRequest
            {
                Version = 1,
                Timestamp = DateTime.UtcNow,
                JobId = job.Id,
                BatchId = job.BatchId,
                DataStandard = job.DataStandard,
                WorkspaceKey = Environment.Configuration["IICWorkspaceKey"]!,
                ExchangeSetNameTemplate = Environment.Configuration["ExchangeSetNameTemplate"]!
            };

            var messageJson = JsonCodec.Encode(request);
            await _queueClient.SendMessageAsync(messageJson, Environment.CancellationToken);

            return NodeResultStatus.Succeeded;
        }
    }
}
