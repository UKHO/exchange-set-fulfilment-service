using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup.Logging;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class ReadConfigurationNode : ExchangeSetPipelineNode
    {
        private const string DebugJobId = "DebugJobId";

        protected override Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var logger = context.Subject.LoggerFactory.CreateLogger<ReadConfigurationNode>();

            var jobId = GetEnvironmentVariable(BuilderEnvironmentVariables.JobId, DebugJobId);
            var workspaceAuthenticationKey = GetEnvironmentVariable(BuilderEnvironmentVariables.WorkspaceKey, "D89D11D265B19CA5C2BE97A7FCB1EF21");

            if (jobId.Equals(DebugJobId, StringComparison.InvariantCultureIgnoreCase))
            {
                logger.LogDebugJobWarning();

                context.Subject.IsDebugSession = true;
                context.Subject.JobId = Guid.NewGuid().ToString("N");
            }
            else
            {
                context.Subject.IsDebugSession = false;
                context.Subject.JobId = jobId;
            }

            var fileShareEndpoint = GetEnvironmentVariable(BuilderEnvironmentVariables.FileShareEndpoint, context.Subject.Configuration.GetValue<string>("Endpoints:FileShareService")!);
            var buildServiceEndpoint = GetEnvironmentVariable(BuilderEnvironmentVariables.BuildServiceEndpoint, context.Subject.Configuration.GetValue<string>("Endpoints:BuildService")!);

            context.Subject.FileShareEndpoint = fileShareEndpoint;
            context.Subject.BuildServiceEndpoint = buildServiceEndpoint;
            context.Subject.WorkspaceAuthenticationKey = workspaceAuthenticationKey;

            var configurationLogView = new ConfigurationLogView()
            {
                JobId = jobId,
                FileShareEndpoint = fileShareEndpoint,
                BuildServiceEndpoint = buildServiceEndpoint,
                WorkspaceAuthenticationKey = workspaceAuthenticationKey,
            };

            logger.LogStartupConfiguration(configurationLogView);

            return Task.FromResult(NodeResultStatus.Succeeded);
        }

        private static string GetEnvironmentVariable(string variable, string overrideValue)
        {
            var value = Environment.GetEnvironmentVariable(variable);

            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (string.IsNullOrEmpty(overrideValue))
            {
                throw new InvalidOperationException($"{variable} is not set");
            }

            return overrideValue;
        }
    }
}
