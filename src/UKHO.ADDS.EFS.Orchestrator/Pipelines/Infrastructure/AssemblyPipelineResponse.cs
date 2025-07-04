using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure
{
    public class AssemblyPipelineResponse
    {
        public int Version { get; init; } = 1;

        public required string JobId { get; init; }

        public required NodeResultStatus Status { get; init; }

        public required ExchangeSetDataStandard DataStandard { get; init; }

        public required string? BatchId { get; init; }
    }
}
