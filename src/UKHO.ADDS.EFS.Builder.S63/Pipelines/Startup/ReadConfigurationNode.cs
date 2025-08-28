using UKHO.ADDS.EFS.Builder.S63.Pipelines.Startup.Logging;
using UKHO.ADDS.EFS.Domain.Builds.S63;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Orchestrator;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Builder.S63.Pipelines.Startup
{
    internal class ReadConfigurationNode : S63ExchangeSetPipelineNode
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S63ExchangeSetPipelineContext> context)
        {
            try
            {
                var logger = context.Subject.LoggerFactory.CreateLogger<ReadConfigurationNode>();
                var configuration = context.Subject.Configuration;

                var requestQueue = context.Subject.QueueClientFactory.CreateRequestQueueClient(context.Subject.Configuration);
                var requestMessage = await requestQueue.ReceiveMessageAsync();

                var request = JsonCodec.Decode<S63BuildRequest>(requestMessage.Value.MessageText)!;

                // TODO Decide on retry strategy for queues and move this as necessary
                await requestQueue.DeleteMessageAsync(requestMessage.Value.MessageId, requestMessage.Value.PopReceipt);

                context.Subject.JobId = request.JobId;
                context.Subject.BatchId = request.BatchId;
                context.Subject.ExchangeSetNameTemplate = request.ExchangeSetNameTemplate;

                var fileShareEndpoint = configuration[BuilderEnvironmentVariables.FileShareEndpoint] ?? configuration["DebugEndpoints:FileShareService"]!;

                context.Subject.FileShareEndpoint = fileShareEndpoint;

                var configurationLogView = new ConfigurationLogView()
                {
                    JobId = context.Subject.JobId,
                    BatchId = context.Subject.BatchId,
                    FileShareEndpoint = fileShareEndpoint,
                    ExchangeSetNameTemplate = context.Subject.ExchangeSetNameTemplate,
                };

                logger.LogStartupConfiguration(configurationLogView);

                return NodeResultStatus.Succeeded;
            }
            catch (Exception ex)
            {
                return NodeResultStatus.Failed;
            }
        }
    }
}
