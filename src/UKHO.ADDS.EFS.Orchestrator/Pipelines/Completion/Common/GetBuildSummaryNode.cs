using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Common
{
    internal class GetBuildSummaryNode : CompletionPipelineNode
    {
        private readonly BuildSummaryTable _summaryTable;

        public GetBuildSummaryNode(BuildSummaryTable summaryTable, NodeEnvironment environment)
            : base(environment) =>
            _summaryTable = summaryTable;

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<CompletionPipelineContext> context)
        {
            var summaryResult = await _summaryTable.GetAsync(context.Subject.JobId, $"{context.Subject.JobId}-summary");

            if (summaryResult.IsSuccess(out var summary))
            {
                context.Subject.BuildSummary = summary;
                return NodeResultStatus.Succeeded;
            }

            return NodeResultStatus.Failed;
        }
    }
}
