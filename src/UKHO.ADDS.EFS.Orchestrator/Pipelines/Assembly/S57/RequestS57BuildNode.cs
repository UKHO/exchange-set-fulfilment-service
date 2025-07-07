using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Jobs.S57;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.S57
{
    internal class RequestS57BuildNode : AssemblyPipelineNode<S57ExchangeSetJob>
    {
        private readonly QueueClient _queueClient;

        public RequestS57BuildNode(NodeEnvironment environment, QueueServiceClient queueServiceClient)
            : base(environment) =>
            _queueClient = queueServiceClient.GetQueueClient(StorageConfiguration.S57BuildRequestQueueName);

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S57ExchangeSetJob> context)
        {
            var job = context.Subject;

            var request = new BuildRequest
            {
                Version = 1,
                Timestamp = DateTime.UtcNow,
                JobId = job.Id,
                BatchId = job.BatchId,
                DataStandard = job.DataStandard,
                WorkspaceKey = "not-used",
                ExchangeSetNameTemplate = Environment.Configuration["S57ExchangeSetNameTemplate"]!
            };

            var messageJson = JsonCodec.Encode(request);
            await _queueClient.SendMessageAsync(messageJson, Environment.CancellationToken);

            return NodeResultStatus.Succeeded;
        }
    }
}
