using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.S100
{
    internal class GetS100JobNode : CompletionPipelineNode
    {
        private readonly S100ExchangeSetJobTable _jobTable;

        public GetS100JobNode(S100ExchangeSetJobTable jobTable, NodeEnvironment environment)
            : base(environment) =>
            _jobTable = jobTable;

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
