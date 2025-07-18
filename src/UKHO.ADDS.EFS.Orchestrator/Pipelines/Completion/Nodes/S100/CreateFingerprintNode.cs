using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Services;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S100
{
    internal class CreateFingerprintNode : CompletionPipelineNode<S100Build>
    {
        private readonly ITable<BuildFingerprint> _buildFingerprintTable;
        private readonly IHashingService _hashingService;

        public CreateFingerprintNode(CompletionNodeEnvironment nodeEnvironment, ITable<BuildFingerprint> buildFingerprintTable, IHashingService hashingService)
            : base(nodeEnvironment)
        {
            _buildFingerprintTable = buildFingerprintTable;
            _hashingService = hashingService;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var enabled = Environment.Configuration.GetValue<bool>("DeduplicationEnabled");
            return Task.FromResult(enabled);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var build = context.Subject.Build;
            var job = context.Subject.Job;

            var discriminant = build.GetProductDiscriminant();
            var discriminantHash = _hashingService.CalculateHash(discriminant);

            // Deduplication is 'best effort' - someone else may have already created a history entry for this job
            // but this one will be later, so update or insert as appropriate. We can't guarantee strict deduplication.

            var history = new BuildFingerprint()
            {
                JobId = job.Id,
                DataStandard = job.DataStandard,
                BatchId = job.BatchId!,
                Timestamp = DateTime.UtcNow,
                Discriminant = discriminantHash
            };

            await _buildFingerprintTable.UpsertAsync(history);

            return NodeResultStatus.Succeeded;
        }
    }
}
