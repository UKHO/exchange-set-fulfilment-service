using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Common
{
    internal class UpdateJobNode<TJob> : CompletionPipelineNode where TJob : ExchangeSetJob
    {
        private readonly ITable<TJob> _jobTable;

        public UpdateJobNode(NodeEnvironment environment, ITable<TJob> jobTable)
            : base(environment)
        {
            _jobTable = jobTable;
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<CompletionPipelineContext> context)
        {
            var jobResult = await _jobTable.GetUniqueAsync(context.Subject.JobId);

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
