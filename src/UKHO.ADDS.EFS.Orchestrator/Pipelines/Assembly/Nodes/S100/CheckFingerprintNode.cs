using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Services;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100
{
    internal class CheckFingerprintNode : AssemblyPipelineNode<S100Build>
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IHashingService _hashingService;

        public CheckFingerprintNode(AssemblyNodeEnvironment nodeEnvironment, IDistributedCache distributedCache, IHashingService hashingService)
            : base(nodeEnvironment)
        {
            _distributedCache = distributedCache;
            _hashingService = hashingService;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var enabled = Environment.Configuration.GetValue<bool>("DeduplicationEnabled");
            return Task.FromResult(enabled && context.Subject.Job.JobState == JobState.Created);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            var discriminant = build.GetProductDiscriminant();
            var discriminantHash = _hashingService.CalculateHash(discriminant);

            var cacheResult = await _distributedCache.GetAsync(discriminantHash);

            if (cacheResult != null)
            {
                var duplicate = JsonCodec.Decode<BuildFingerprint>(Encoding.UTF8.GetString(cacheResult))!;
                job.BatchId = duplicate.BatchId;

                await context.Subject.SignalBuildDuplicated();
            }

            return NodeResultStatus.Succeeded;
        }
    }
}
