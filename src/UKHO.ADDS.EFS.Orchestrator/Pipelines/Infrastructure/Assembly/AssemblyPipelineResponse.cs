using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly
{
    public class AssemblyPipelineResponse
    {
        public int Version { get; init; } = 1;

        public required string JobId { get; init; }

        public required JobState JobStatus { get; init; }

        public required BuildState BuildStatus { get; init; }

        public required DataStandard DataStandard { get; init; }

        public required string? BatchId { get; init; }

        /// <summary>
        /// Error response model containing validation errors, if any
        /// </summary>
        public ErrorResponseModel? ErrorResponse { get; init; }

        /// <summary>
        /// Success response data for the request, if no errors
        /// </summary>
        public object? ResponseData { get; init; }
    }
}
