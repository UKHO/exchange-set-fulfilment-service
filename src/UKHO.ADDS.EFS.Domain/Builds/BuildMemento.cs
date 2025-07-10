using UKHO.ADDS.EFS.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.Builds
{
    public class BuildMemento
    {
        private List<BuildNodeStatus> _nodeStatuses;

        public BuildMemento()
        {
            _nodeStatuses = [];
        }

        public required string JobId { get; init; } 

        public IEnumerable<BuildNodeStatus>? BuilderSteps
        {
            get => _nodeStatuses;
            set => _nodeStatuses = value?.ToList() ?? [];
        }

        public required BuilderExitCode BuilderExitCode { get; init; }
    }
}
