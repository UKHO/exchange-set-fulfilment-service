using Serilog;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup.Logging;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class ReadConfigurationNode : S100ExchangeSetPipelineNode
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S100ExchangeSetPipelineContext> context)
        {
            try
            {
                var logger = context.Subject.LoggerFactory.CreateLogger<ReadConfigurationNode>();
                var configuration = context.Subject.Configuration;

                var requestQueue = context.Subject.QueueClientFactory.CreateRequestQueueClient(context.Subject.Configuration);
                var requestMessage = await requestQueue.ReceiveMessageAsync();

                var request = JsonCodec.Decode<BuildRequest>(requestMessage.Value.MessageText)!;

                // TODO Decide on retry strategy for queues and move this as necessary
                await requestQueue.DeleteMessageAsync(requestMessage.Value.MessageId, requestMessage.Value.PopReceipt);

                context.Subject.JobId = request.JobId;
                context.Subject.BatchId = request.BatchId;
                context.Subject.WorkspaceAuthenticationKey = request.WorkspaceKey;
                context.Subject.ExchangeSetNameTemplate = request.ExchangeSetNameTemplate;

                var fileShareEndpoint = configuration[BuilderEnvironmentVariables.FileShareEndpoint] ?? configuration["DebugEndpoints:FileShareService"]!;
                var fileShareHealthEndpoint = configuration[BuilderEnvironmentVariables.FileShareHealthEndpoint] ?? configuration["DebugEndpoints:FileShareServiceHealth"]!;

                context.Subject.FileShareEndpoint = fileShareEndpoint;
                context.Subject.FileShareHealthEndpoint = fileShareHealthEndpoint;

                var configurationLogView = new ConfigurationLogView()
                {
                    JobId = context.Subject.JobId,
                    BatchId = context.Subject.BatchId,
                    FileShareEndpoint = fileShareEndpoint,
                    FileShareHealthEndpoint = fileShareHealthEndpoint,
                    WorkspaceAuthenticationKey = context.Subject.WorkspaceAuthenticationKey,
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
