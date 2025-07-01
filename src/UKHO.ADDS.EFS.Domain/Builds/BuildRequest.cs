using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Builds
{
    public class BuildRequest
    {
        public required int Version { get; init; }

        public DateTime Timestamp { get; init; }

        public required string JobId { get; init; }

        public required string BatchId { get; init; }

        public required ExchangeSetDataStandard DataStandard { get; init; }

        public required string WorkspaceKey { get; init; }

        public required string ExchangeSetNameTemplate { get; init; }
    }
}
