using UKHO.ADDS.EFS.VOS;

namespace UKHO.ADDS.EFS.Builder.S63.Pipelines.Startup.Logging
{
    internal class ConfigurationLogView
    {

        public required JobId JobId { get; init; }
        public required BatchId BatchId { get; init; }
        public required string FileShareEndpoint { get; init; }
        public required string ExchangeSetNameTemplate { get; init; }
    }
}
