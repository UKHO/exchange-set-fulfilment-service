namespace UKHO.ADDS.EFS.Builds
{
    public class BuildResponse
    {
        public required string JobId { get; init; }

        public required int ExitCode { get; init; }
    }
}
