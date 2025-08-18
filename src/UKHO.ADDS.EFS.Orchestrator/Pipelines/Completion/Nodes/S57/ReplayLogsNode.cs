using Serilog.Context;
using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S57
{
    internal class ReplayLogsNode : CompletionPipelineNode<S57Build>
    {
        private readonly IBuilderLogForwarder _logForwarder;

        public ReplayLogsNode(CompletionNodeEnvironment nodeEnvironment, IBuilderLogForwarder logForwarder)
            : base(nodeEnvironment)
        {
            _logForwarder = logForwarder;
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S57Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            // Ensure correlation ID is in LogContext when forwarding logs
            using (LogContext.PushProperty("CorrelationId", job.GetCorrelationId()))
            {
                await _logForwarder.ForwardLogsAsync(build.LogMessages!, DataStandard.S57, job.Id);
            }

            return NodeResultStatus.Succeeded;
        }
    }
}
