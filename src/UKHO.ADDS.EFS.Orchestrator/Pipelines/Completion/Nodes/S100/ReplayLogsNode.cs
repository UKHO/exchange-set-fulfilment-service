using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S100
{
    internal class ReplayLogsNode : CompletionPipelineNode<S100Build>
    {
        private readonly IBuilderLogForwarder _logForwarder;

        public ReplayLogsNode(CompletionNodeEnvironment nodeEnvironment, IBuilderLogForwarder logForwarder)
            : base(nodeEnvironment)
        {
            _logForwarder = logForwarder;
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            await _logForwarder.ForwardLogsAsync(build.LogMessages!, DataStandard.S100, job.Id);

            return NodeResultStatus.Succeeded;
        }
    }
}
