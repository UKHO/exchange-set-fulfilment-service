using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.S100
{
    internal class GetS100BuildSummaryNode : CompletionPipelineNode
    {
        private readonly S100BuildSummaryTable _summaryTable;

        public GetS100BuildSummaryNode(S100BuildSummaryTable summaryTable, NodeEnvironment environment)
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
