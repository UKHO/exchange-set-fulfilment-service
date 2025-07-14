using UKHO.ADDS.EFS.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.Builds
{
    public class BuildResponse
    {
        public required string JobId { get; init; }

        public required BuilderExitCode ExitCode { get; init; }
    }
}
