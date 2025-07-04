using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Services2.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Common
{
    internal class ExpireOldFileShareBatchesNode : CompletionPipelineNode
    {
        private readonly FileShareService _fileShareService;
        private readonly ExchangeSetTimestampTable _timestampTable;

        public ExpireOldFileShareBatchesNode(FileShareService fileShareService, ExchangeSetTimestampTable timestampTable, NodeEnvironment environment)
            : base(environment)
        {
            _fileShareService = fileShareService;
            _timestampTable = timestampTable;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<CompletionPipelineContext> context) => Task.FromResult(context.Subject.Job != null && !string.IsNullOrEmpty(context.Subject.Job.BatchId));

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<CompletionPipelineContext> context)
        {
            var job = context.Subject.Job!;

            var searchResult = await _fileShareService.SearchCommittedBatchesExcludingCurrentAsync(job.BatchId, job.GetCorrelationId(), Environment.CancellationToken);
            if (!searchResult.IsSuccess(out var searchResponse, out _))
            {
                return NodeResultStatus.Failed;
            }

            if (searchResponse?.Entries == null || searchResponse.Entries.Count == 0)
            {
                return NodeResultStatus.Succeeded;
            }

            var expiryResult = await _fileShareService.SetExpiryDateAsync(searchResponse.Entries, job.GetCorrelationId(), Environment.CancellationToken);
            job.State = expiryResult.IsSuccess(out _, out _)
                ? ExchangeSetJobState.Succeeded
                : ExchangeSetJobState.Failed;

            if (job.State == ExchangeSetJobState.Succeeded)
            {
                var updateTimestampEntity = new ExchangeSetTimestamp { DataStandard = job.DataStandard, Timestamp = job.SalesCatalogueTimestamp };

                await _timestampTable.UpsertAsync(updateTimestampEntity);
            }

            return NodeResultStatus.Succeeded;
        }
    }
}
