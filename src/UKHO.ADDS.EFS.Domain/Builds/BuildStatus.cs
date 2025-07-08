using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.NewEFS;

namespace UKHO.ADDS.EFS.Builds
{
    public class BuildStatus
    {
        public required string JobId { get; init; }

        public DateTime StartTimestamp { get; set; }

        public DateTime? EndTimestamp { get; set; }

        public required DataStandard DataStandard { get; init; }

        public BuilderExitCode ExitCode { get; set; }

        // TODO IEnumerable - configure serialization
        public List<BuildNodeStatus> Nodes { get; set; } = [];
    }
}
