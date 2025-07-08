using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Common
{
    internal class ReplayLogsNode : CompletionPipelineNode
    {
        private readonly BuilderLogForwarder _logForwarder;

        public ReplayLogsNode(NodeEnvironment environment, BuilderLogForwarder logForwarder)
            : base(environment) =>
            _logForwarder = logForwarder;

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<CompletionPipelineContext> context) => Task.FromResult(context.Subject.BuildSummary is { LogMessages: not null });

        protected override Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<CompletionPipelineContext> context)
        {
            _logForwarder.ForwardLogs(context.Subject.BuildSummary!.LogMessages!, context.Subject.DataStandard, context.Subject.JobId);

            return Task.FromResult(NodeResultStatus.Succeeded);
        }
    }
}
