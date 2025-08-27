using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.VOS;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly
{
    public class AssemblyPipelineResponse
    {
        public MessageVersion Version { get; init; } = MessageVersion.From(1);

        public required JobId JobId { get; init; }

        public required JobState JobStatus { get; init; }

        public required BuildState BuildStatus { get; init; }

        public required DataStandard DataStandard { get; init; }

        public required BatchId BatchId { get; init; }
    }
}
