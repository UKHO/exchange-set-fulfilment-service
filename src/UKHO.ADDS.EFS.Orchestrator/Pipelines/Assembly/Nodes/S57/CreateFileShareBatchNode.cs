using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.EFS.VOS;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S57
{
    internal class CreateFileShareBatchNode : AssemblyPipelineNode<S57Build>
    {
        private readonly IOrchestratorFileShareClient _fileShareClient;

        public CreateFileShareBatchNode(AssemblyNodeEnvironment nodeEnvironment, IOrchestratorFileShareClient fileShareClient)
            : base(nodeEnvironment)
        {
            _fileShareClient = fileShareClient;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S57Build>> context)
        {
            return Task.FromResult(context.Subject.Job.JobState == JobState.Created);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S57Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            var createBatchResponseResult = await _fileShareClient.CreateBatchAsync((string)job.GetCorrelationId(), Environment.CancellationToken);

            if (createBatchResponseResult.IsSuccess(out var batchHandle, out _))
            {
                job.BatchId = BatchId.From(batchHandle.BatchId);
                build.BatchId = BatchId.From(batchHandle.BatchId);
            }
            else
            {
                // Could not create a batch, so the job should fail
                await context.Subject.SignalAssemblyError();
            }

            return NodeResultStatus.Succeeded;
        }
    }
}
