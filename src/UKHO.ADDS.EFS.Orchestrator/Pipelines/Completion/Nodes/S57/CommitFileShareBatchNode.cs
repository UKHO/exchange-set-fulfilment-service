using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S57;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S57
{
    internal class CommitFileShareBatchNode : CompletionPipelineNode<S57Build>
    {
        private readonly IFileService _fileService;

        public CommitFileShareBatchNode(CompletionNodeEnvironment nodeEnvironment, IFileService fileService)
            : base(nodeEnvironment)
        {
            _fileService = fileService;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S57Build>> context)
        {
            return Task.FromResult(context.Subject.Job.BatchId != BatchId.None && Environment.BuilderExitCode == BuilderExitCode.Success);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S57Build>> context)
        {
            var job = context.Subject.Job!;

            var batchHandle = new BatchHandle((string)job.BatchId!);

            var commitBatchResult = await _fileService.CommitBatchAsync(batchHandle, (string)job.GetCorrelationId(), Environment.CancellationToken);

            if (!commitBatchResult.IsSuccess(out _, out _))
            {
                return NodeResultStatus.Failed;
            }

            return NodeResultStatus.Succeeded;
        }
    }
}
