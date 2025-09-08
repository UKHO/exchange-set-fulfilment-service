using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S100
{
    internal class CommitFileShareBatchNode : CompletionPipelineNode<S100Build>
    {
        private readonly IFileService _fileService;

        public CommitFileShareBatchNode(CompletionNodeEnvironment nodeEnvironment, IFileService fileService)
            : base(nodeEnvironment)
        {
            _fileService = fileService;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            return Task.FromResult(context.Subject.Job.BatchId != BatchId.None && (Environment.BuilderExitCode == BuilderExitCode.Success || context.Subject.IsErrorFileCreated));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job!;
            var buildCommitInfo = context.Subject.Build.BuildCommitInfo;

            var batchHandle = new BatchHandle((string)job.BatchId!);

            // Add file details to the batch handle for validation during commit
            foreach (var fileDetail in buildCommitInfo!.FileDetails)
            {
                batchHandle.AddFile(fileDetail.FileName, fileDetail.Hash);
            }

            var commitBatchResult = await _fileService.CommitBatchAsync(batchHandle, (string)job.GetCorrelationId(), Environment.CancellationToken);

            if (!commitBatchResult.IsSuccess(out _, out _))
            {
                return NodeResultStatus.Failed;
            }
            
            return NodeResultStatus.Succeeded;
        }
    }
}
