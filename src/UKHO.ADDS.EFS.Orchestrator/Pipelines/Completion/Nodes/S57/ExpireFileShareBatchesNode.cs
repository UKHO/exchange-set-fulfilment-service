using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Services;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.EFS.VOS;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S57
{
    internal class ExpireFileShareBatchesNode : CompletionPipelineNode<S57Build>
    {
        private readonly IOrchestratorFileShareClient _fileShareClient;
        private readonly ITimestampService _timestampService;

        public ExpireFileShareBatchesNode(CompletionNodeEnvironment nodeEnvironment, IOrchestratorFileShareClient fileShareClient, ITimestampService timestampService)
            : base(nodeEnvironment)
        {
            _fileShareClient = fileShareClient;
            _timestampService = timestampService;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S57Build>> context)
        {
            return Task.FromResult(context.Subject.Job.BatchId != BatchId.None && Environment.BuilderExitCode == BuilderExitCode.Success);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S57Build>> context)
        {
            var job = context.Subject.Job!;

            var searchResult = await _fileShareClient.SearchCommittedBatchesExcludingCurrentAsync((string)job.BatchId, (string)job.GetCorrelationId(), Environment.CancellationToken);
            if (!searchResult.IsSuccess(out var searchResponse, out _))
            {
                return NodeResultStatus.Failed;
            }

            if (searchResponse?.Entries == null || searchResponse.Entries.Count == 0)
            {
                return NodeResultStatus.Succeeded;
            }

            // TODO State management

            var expiryResult = await _fileShareClient.SetExpiryDateAsync(searchResponse.Entries, (string)job.GetCorrelationId(), Environment.CancellationToken);

            await _timestampService.SetTimestampForJobAsync(job);

            return NodeResultStatus.Succeeded;
        }
    }
}
