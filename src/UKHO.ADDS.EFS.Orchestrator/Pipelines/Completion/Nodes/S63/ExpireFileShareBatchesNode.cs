using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S63;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S63
{
    internal class ExpireFileShareBatchesNode : CompletionPipelineNode<S63Build>
    {
        private readonly IFileService _fileService;
        private readonly ITimestampService _timestampService;

        public ExpireFileShareBatchesNode(CompletionNodeEnvironment nodeEnvironment, IFileService fileService, ITimestampService timestampService)
            : base(nodeEnvironment)
        {
            _fileService = fileService;
            _timestampService = timestampService;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S63Build>> context)
        {
            return Task.FromResult(context.Subject.Job.BatchId != BatchId.None && Environment.BuilderExitCode == BuilderExitCode.Success);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S63Build>> context)
        {
            var job = context.Subject.Job!;

            var searchResult = await _fileService.SearchCommittedBatchesExcludingCurrentAsync((string)job.BatchId, (string)job.GetCorrelationId(), Environment.CancellationToken);
            if (!searchResult.IsSuccess(out var searchResponse, out _))
            {
                return NodeResultStatus.Failed;
            }

            if (searchResponse?.Entries == null || searchResponse.Entries.Count == 0)
            {
                return NodeResultStatus.Succeeded;
            }

            // TODO State management

            var expiryResult = await _fileService.SetExpiryDateAsync(searchResponse.Entries, (string)job.GetCorrelationId(), Environment.CancellationToken);

            await _timestampService.SetTimestampForJobAsync(job);

            return NodeResultStatus.Succeeded;
        }
    }
}
