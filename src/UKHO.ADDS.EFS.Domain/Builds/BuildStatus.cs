using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Builds
{
    public class BuildStatus
    {
        public required string JobId { get; init; }

        public required ExchangeSetDataStandard DataStandard { get; init; }

        public BuilderExitCode ExitCode { get; set; }

        public IEnumerable<BuildNodeStatus> Nodes { get; set; } = Enumerable.Empty<BuildNodeStatus>();
    }
}
