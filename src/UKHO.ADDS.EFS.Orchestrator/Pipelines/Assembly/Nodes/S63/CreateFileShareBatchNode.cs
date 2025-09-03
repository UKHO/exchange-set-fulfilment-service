using UKHO.ADDS.EFS.Domain.Builds.S63;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S63
{
    internal class CreateFileShareBatchNode : AssemblyPipelineNode<S63Build>
    {
        private readonly IFileService _fileService;

        public CreateFileShareBatchNode(AssemblyNodeEnvironment nodeEnvironment, IFileService fileService)
            : base(nodeEnvironment)
        {
            _fileService = fileService;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S63Build>> context)
        {
            return Task.FromResult(context.Subject.Job.JobState == JobState.Created);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S63Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            var createBatchResponseResult = await _fileService.CreateBatchAsync((string)job.GetCorrelationId(), Environment.CancellationToken);

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
