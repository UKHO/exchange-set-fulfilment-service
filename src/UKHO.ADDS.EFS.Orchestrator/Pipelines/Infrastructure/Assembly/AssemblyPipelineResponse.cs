using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Messages;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly
{
    public class AssemblyPipelineResponse
    {
        public MessageVersion Version { get; init; } = MessageVersion.From(1);

        public required JobId JobId { get; init; }

        public required JobState JobStatus { get; init; }

        public required BuildState BuildStatus { get; init; }

        public required DataStandard DataStandard { get; init; }

        public required BatchId BatchId { get; init; }

        /// <summary>
        /// Error response model containing validation errors, if any
        /// </summary>
        internal ErrorResponseModel? ErrorResponse { get; init; }

        /// <summary>
        /// Success response data for the request, if no errors
        /// </summary>
        internal CustomExchangeSetResponse? Response { get; init; }
    }
}
