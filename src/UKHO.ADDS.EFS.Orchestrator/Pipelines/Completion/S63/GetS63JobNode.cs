using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S63;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.S63
{
    internal class GetS63JobNode : CompletionPipelineNode
    {
        private readonly S63ExchangeSetJobTable _jobTable;

        public GetS63JobNode(S63ExchangeSetJobTable jobTable, NodeEnvironment environment)
            : base(environment)
        {
            _jobTable = jobTable;
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<CompletionPipelineContext> context)
        {
            var jobResult = await _jobTable.GetAsync(context.Subject.JobId, context.Subject.JobId);

            if (jobResult.IsSuccess(out var existingJob))
            {
                context.Subject.Job = existingJob;
                return NodeResultStatus.Succeeded;
            }

            return NodeResultStatus.Failed;
        }
    }
}
