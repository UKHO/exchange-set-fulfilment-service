namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup.Logging
{
    internal class ConfigurationLogView
    {

        public required string JobId { get; init; }
        public required string BatchId { get; init; }
        public required string FileShareEndpoint { get; init; }
        public required string BuildServiceEndpoint { get; init; }

        public required string WorkspaceAuthenticationKey { get; init; }
    }
}
