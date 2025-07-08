using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Common
{
    internal class GetJobNode<TJob> : CompletionPipelineNode where TJob : ExchangeSetJob
    {
        private readonly ITable<TJob> _jobTable;

        public GetJobNode(NodeEnvironment environment, ITable<TJob> jobTable)
            : base(environment) =>
            _jobTable = jobTable;

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<CompletionPipelineContext> context)
        {
            var jobResult = await _jobTable.GetUniqueAsync(context.Subject.JobId);

            if (jobResult.IsSuccess(out var existingJob))
            {
                context.Subject.Job = existingJob;
                return NodeResultStatus.Succeeded;
            }

            return NodeResultStatus.Failed;
        }
    }
}
