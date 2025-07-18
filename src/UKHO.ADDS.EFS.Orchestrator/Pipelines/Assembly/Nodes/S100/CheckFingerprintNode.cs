using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Services;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100
{
    internal class CheckFingerprintNode : AssemblyPipelineNode<S100Build>
    {
        private readonly IHashingService _hashingService;
        private readonly ITable<BuildFingerprint> _buildFingerprintTable;

        public CheckFingerprintNode(AssemblyNodeEnvironment nodeEnvironment, ITable<BuildFingerprint> buildFingerprintTable, IHashingService hashingService)
            : base(nodeEnvironment)
        {
            _hashingService = hashingService;
            _buildFingerprintTable = buildFingerprintTable;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var enabled = Environment.Configuration.GetValue<bool>("DeduplicationEnabled");
            return Task.FromResult(enabled && context.Subject.Job.JobState == JobState.Created);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var expiry = Environment.Configuration.GetValue<TimeSpan>("DeduplicationExpiry");

            var job = context.Subject.Job;
            var build = context.Subject.Build;

            var discriminant = build.GetProductDiscriminant();
            var discriminantHash = _hashingService.CalculateHash(discriminant);

            var hasDuplicateResult = await _buildFingerprintTable.GetUniqueAsync(discriminantHash);

            if (hasDuplicateResult.IsSuccess(out var duplicate))
            {
                var expiryThreshold = DateTime.UtcNow - expiry;

                if (duplicate.Timestamp > expiryThreshold)
                {
                    job.BatchId = duplicate.BatchId;

                    await context.Subject.SignalBuildDuplicated();
                }
            }

            return NodeResultStatus.Succeeded;
        }
    }
}
