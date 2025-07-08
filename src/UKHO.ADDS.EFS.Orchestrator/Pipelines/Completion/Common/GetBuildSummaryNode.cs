using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Common
{
    internal class GetBuildSummaryNode<TSummary> : CompletionPipelineNode where TSummary : BuildSummary
    {
        private readonly ITable<TSummary> _summaryTable;

        public GetBuildSummaryNode(NodeEnvironment environment, ITable<TSummary> summaryTable)
            : base(environment) =>
            _summaryTable = summaryTable;

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<CompletionPipelineContext> context)
        {
            var summaryResult = await _summaryTable.GetUniqueAsync(context.Subject.JobId, $"{context.Subject.JobId}-summary");

            if (summaryResult.IsSuccess(out var summary))
            {
                context.Subject.BuildSummary = summary;
                return NodeResultStatus.Succeeded;
            }

            return NodeResultStatus.Failed;
        }
    }
}
