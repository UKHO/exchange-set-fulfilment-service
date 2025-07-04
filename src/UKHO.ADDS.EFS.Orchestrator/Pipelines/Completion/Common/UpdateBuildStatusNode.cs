using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Common
{
    internal class UpdateBuildStatusNode : CompletionPipelineNode
    {
        private readonly BuildStatusTable _statusTable;

        public UpdateBuildStatusNode(BuildStatusTable statusTable, NodeEnvironment environment)
            : base(environment) =>
            _statusTable = statusTable;

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<CompletionPipelineContext> context) => Task.FromResult(context.Subject.BuildStatus != null && context.Subject.BuildSummary != null);

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<CompletionPipelineContext> context)
        {
            context.Subject.BuildStatus!.ExitCode = context.Subject.ExitCode;
            context.Subject.BuildStatus!.EndTimestamp = DateTime.UtcNow;

            context.Subject.BuildStatus!.Nodes.AddRange(context.Subject.BuildSummary!.Statuses!);

            await _statusTable.UpdateAsync(context.Subject.BuildStatus);
            return NodeResultStatus.Succeeded;
        }
    }
}
