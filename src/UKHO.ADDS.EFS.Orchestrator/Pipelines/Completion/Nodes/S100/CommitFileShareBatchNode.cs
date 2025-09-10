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

            if (batchHandle.FileDetails?.Count > 0)
            {
                var firstFileDetail = batchHandle.FileDetails.First();
                context.Subject.Build.BuildCommitInfo.AddFileDetail(firstFileDetail.FileName, firstFileDetail.Hash);
            }

            try
            {
                var commitBatchResult = await _fileService.CommitBatchAsync(batchHandle, job.GetCorrelationId(), Environment.CancellationToken);
                return NodeResultStatus.Succeeded;
            }
            catch (Exception)
            {
                return NodeResultStatus.Failed;
            }
        }
    }
}
