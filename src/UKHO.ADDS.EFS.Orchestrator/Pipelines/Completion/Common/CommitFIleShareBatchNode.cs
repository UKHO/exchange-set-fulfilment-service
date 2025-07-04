using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Common
{
    internal class CommitFileShareBatchNode : CompletionPipelineNode
    {
        private readonly FileShareService _fileShareService;

        public CommitFileShareBatchNode(FileShareService fileShareService, NodeEnvironment environment)
            : base(environment) =>
            _fileShareService = fileShareService;

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<CompletionPipelineContext> context) => Task.FromResult(context.Subject.Job != null && !string.IsNullOrEmpty(context.Subject.Job.BatchId));

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<CompletionPipelineContext> context)
        {
            var job = context.Subject.Job!;

            // Try to commit the batch
            var commitBatchResult = await _fileShareService.CommitBatchAsync(job.BatchId, job.GetCorrelationId(), Environment.CancellationToken);
            if (!commitBatchResult.IsSuccess(out _, out _))
            {
                return NodeResultStatus.Failed;
            }

            return NodeResultStatus.Succeeded;
        }
    }
}
