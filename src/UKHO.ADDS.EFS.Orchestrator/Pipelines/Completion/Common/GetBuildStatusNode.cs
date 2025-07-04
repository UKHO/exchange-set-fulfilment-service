using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Common
{
    internal class GetBuildStatusNode : CompletionPipelineNode
    {
        private readonly BuildStatusTable _statusTable;

        public GetBuildStatusNode(BuildStatusTable statusTable, NodeEnvironment environment)
            : base(environment) =>
            _statusTable = statusTable;

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<CompletionPipelineContext> context)
        {
            var existingStatusResponse = await _statusTable.GetAsync(context.Subject.JobId, context.Subject.JobId);

            if (existingStatusResponse.IsSuccess(out var existingStatus))
            {
                context.Subject.BuildStatus = existingStatus;
                return NodeResultStatus.Succeeded;
            }

            return NodeResultStatus.Failed;
        }
    }
}
