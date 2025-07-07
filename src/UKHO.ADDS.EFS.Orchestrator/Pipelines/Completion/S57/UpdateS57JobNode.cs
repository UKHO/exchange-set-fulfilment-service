using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S57;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.S57
{
    internal class UpdateS57JobNode : CompletionPipelineNode
    {
        private readonly S57ExchangeSetJobTable _jobTable;

        public UpdateS57JobNode(S57ExchangeSetJobTable jobTable, NodeEnvironment environment)
            : base(environment)
        {
            _jobTable = jobTable;
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<CompletionPipelineContext> context)
        {
            var jobResult = await _jobTable.GetAsync(context.Subject.JobId, context.Subject.JobId);

            if (jobResult.IsSuccess(out var existingJob))
            {
                existingJob.State = context.Subject.ExitCode switch
                {
                    BuilderExitCode.Success => ExchangeSetJobState.Succeeded,
                    BuilderExitCode.Failed => ExchangeSetJobState.Failed,
                    BuilderExitCode.NotRun => ExchangeSetJobState.Failed,
                    _ => existingJob.State
                };

                await _jobTable.UpdateAsync(existingJob);

                return NodeResultStatus.Succeeded;
            }

            return NodeResultStatus.Failed;
        }
    }
}
