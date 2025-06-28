using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Builds
{
    public class BuildRequest
    {
        public required string JobId { get; init; }

        public required string BatchId { get; init; }

        public required ExchangeSetDataStandard DataStandard { get; init; }

        public required string FileShareServiceUri { get; init; }

        public required string WorkspaceKey { get; init; }
    }
}
