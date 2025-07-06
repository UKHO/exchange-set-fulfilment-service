using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S57;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.S57
{
    internal class GetS57JobNode : CompletionPipelineNode
    {
        private readonly S57ExchangeSetJobTable _jobTable;

        public GetS57JobNode(S57ExchangeSetJobTable jobTable, NodeEnvironment environment)
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
