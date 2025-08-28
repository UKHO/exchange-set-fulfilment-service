using UKHO.ADDS.EFS.Domain.Jobs;

namespace UKHO.ADDS.EFS.Domain.Builds
{
    public class BuildResponse
    {
        public required JobId JobId { get; init; }

        public required BuilderExitCode ExitCode { get; init; }
    }
}
