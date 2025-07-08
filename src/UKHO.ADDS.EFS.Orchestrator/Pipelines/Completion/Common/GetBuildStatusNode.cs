using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Common
{
    internal class GetBuildStatusNode : CompletionPipelineNode
    {
        private readonly ITable<BuildStatus> _statusTable;

        public GetBuildStatusNode(NodeEnvironment environment, ITable<BuildStatus> statusTable)
            : base(environment) =>
            _statusTable = statusTable;

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<CompletionPipelineContext> context)
        {
            var existingStatusResponse = await _statusTable.GetUniqueAsync(context.Subject.JobId);

            if (existingStatusResponse.IsSuccess(out var existingStatus))
            {
                context.Subject.BuildStatus = existingStatus;
                return NodeResultStatus.Succeeded;
            }

            return NodeResultStatus.Failed;
        }
    }
}
