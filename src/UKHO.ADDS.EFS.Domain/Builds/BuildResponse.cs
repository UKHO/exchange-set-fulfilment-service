using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.EFS.Builds
{
    public class BuildResponse
    {
        public required JobId JobId { get; init; }

        public required BuilderExitCode ExitCode { get; init; }
    }
}
