using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure
{
    public class AssemblyPipelineResponse
    {
        public int Version { get; init; } = 1;

        public required string JobId { get; init; }

        // TODO NO!!! 
        public required NodeResultStatus Status { get; init; }

        public required DataStandard DataStandard { get; init; }

        public required string? BatchId { get; init; }
    }
}
