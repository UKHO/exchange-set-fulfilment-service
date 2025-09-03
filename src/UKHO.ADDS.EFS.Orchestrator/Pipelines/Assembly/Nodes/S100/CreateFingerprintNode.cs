using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Services;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100
{
    internal class CreateFingerprintNode : AssemblyPipelineNode<S100Build>
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IHashingService _hashingService;

        public CreateFingerprintNode(AssemblyNodeEnvironment nodeEnvironment, IDistributedCache distributedCache, IHashingService hashingService)
            : base(nodeEnvironment)
        {
            _distributedCache = distributedCache;
            _hashingService = hashingService;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var enabled = Environment.Configuration.GetValue<bool>("orchestrator:Deduplication:Enabled");
            return Task.FromResult(enabled && context.Subject.Job.JobState == JobState.Submitted);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var build = context.Subject.Build;
            var job = context.Subject.Job;

            var discriminant = build.GetProductDiscriminant();
            var discriminantHash = _hashingService.CalculateHash(discriminant);

            // Deduplication is 'best effort', we can't guarantee to catch all of them

            var history = new BuildFingerprint()
            {
                JobId = job.Id,
                DataStandard = job.DataStandard,
                BatchId = job.BatchId!,
                Timestamp = DateTime.UtcNow,
                Discriminant = discriminantHash
            };

            var historyJson = JsonCodec.Encode(history);

            var expiry = Environment.Configuration.GetValue<TimeSpan>("orchestrator:Deduplication:Expiry");

            await _distributedCache.SetAsync(discriminantHash, Encoding.UTF8.GetBytes(historyJson), new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = expiry
            });

            return NodeResultStatus.Succeeded;
        }
    }
}
