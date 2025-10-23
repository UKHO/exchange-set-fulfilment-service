using System.Net;
using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.External;
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

        /// <summary>
        /// Gets or sets the HTTP status code representing the response from the SCS service.
        /// </summary>
        [JsonIgnore]
        public HttpStatusCode ExternalApiResponseCode { get; init; }

        /// <summary>
        /// Gets or sets the date and time when the entity was last modified in the SCS system.
        /// </summary>
        public DateTime? ProductsLastModified { get; init; }

        /// <summary>
        /// Gets the name of the external API service.    
        /// </summary>
        [JsonIgnore]
        public ExternalServiceName ExternalApiServiceName { get; init; } = ExternalServiceName.NotDefined;
    }
}
