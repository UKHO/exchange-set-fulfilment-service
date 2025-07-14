namespace UKHO.ADDS.EFS.Builder.S57.Pipelines.Startup.Logging
{
    internal class ConfigurationLogView
    {
        public required string JobId { get; init; }
        public required string BatchId { get; init; }
        public required string FileShareEndpoint { get; init; }
        public required string ExchangeSetNameTemplate { get; init; }
    }
}
