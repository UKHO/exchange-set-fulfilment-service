using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Jobs.S63;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.S63
{
    internal class RequestS63BuildNode : AssemblyPipelineNode<S63ExchangeSetJob>
    {
        private readonly QueueClient _queueClient;

        public RequestS63BuildNode(NodeEnvironment environment, QueueServiceClient queueServiceClient)
            : base(environment) =>
            _queueClient = queueServiceClient.GetQueueClient(StorageConfiguration.S63BuildRequestQueueName);

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S63ExchangeSetJob> context)
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
                ExchangeSetNameTemplate = Environment.Configuration["S63ExchangeSetNameTemplate"]!
            };

            var messageJson = JsonCodec.Encode(request);
            await _queueClient.SendMessageAsync(messageJson, Environment.CancellationToken);

            return NodeResultStatus.Succeeded;
        }
    }
}
