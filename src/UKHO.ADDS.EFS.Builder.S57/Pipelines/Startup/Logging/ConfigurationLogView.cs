using UKHO.ADDS.EFS.Domain.Jobs;

namespace UKHO.ADDS.EFS.Builder.S57.Pipelines.Startup.Logging
{
    internal class ConfigurationLogView
    {
        public required JobId JobId { get; init; }
        public required BatchId BatchId { get; init; }
        public required string FileShareEndpoint { get; init; }
        public required string ExchangeSetNameTemplate { get; init; }
    }
}
