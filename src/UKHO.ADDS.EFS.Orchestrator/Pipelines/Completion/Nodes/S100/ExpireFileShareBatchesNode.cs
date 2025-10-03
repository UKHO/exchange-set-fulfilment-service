using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S100
{
    internal class ExpireFileShareBatchesNode : CompletionPipelineNode<S100Build>
    {
        private readonly IFileService _fileService;
        private readonly ITimestampService _timestampService;

        public ExpireFileShareBatchesNode(CompletionNodeEnvironment nodeEnvironment, IFileService fileService, ITimestampService timestampService)
            : base(nodeEnvironment)
        {
            _fileService = fileService;
            _timestampService = timestampService;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            return Task.FromResult(context.Subject.Job.BatchId != BatchId.None && Environment.BuilderExitCode == BuilderExitCode.Success && context.Subject.Job.ExchangeSetType == ExchangeSetType.Complete);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job!;

            try
            {
                var searchResult = await _fileService.SearchCommittedBatchesExcludingCurrentAsync(job.BatchId, job.GetCorrelationId(), Environment.CancellationToken);

                if (searchResult?.Entries == null || searchResult.Entries.Count == 0)
                {
                    return NodeResultStatus.Succeeded;
                }

                var expiryResult = await _fileService.SetExpiryDateAsync(searchResult.Entries, job.GetCorrelationId(), Environment.CancellationToken);

                await _timestampService.SetTimestampForJobAsync(job);
            }
            catch (Exception ex)
            {
                return NodeResultStatus.Failed;
            }

            // TODO State management

            return NodeResultStatus.Succeeded;
        }
    }
}
