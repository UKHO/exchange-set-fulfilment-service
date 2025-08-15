using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly
{
    internal class AssemblyPipelineParameters
    {
        public required int Version { get; init; }

        public required DateTime Timestamp { get; init; }

        public required DataStandard DataStandard { get; init; }

        public required string[] Products { get; init; }

        public required string Filter { get; init; }

        public required string JobId { get; init; }

        public required IConfiguration Configuration { get; init; }

        public Job CreateJob()
        {
            // Validate and filter the products array
            var validatedProducts = Products?.Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .ToArray() ?? [];

            return new Job()
            {
                Id = JobId,
                Timestamp = Timestamp,
                DataStandard = DataStandard,
                RequestedProducts = validatedProducts,
                RequestedFilter = Filter
            };
        }

        public static AssemblyPipelineParameters CreateFrom(JobRequestApiMessage message, IConfiguration configuration, string correlationId) =>
            new()
            {
                Version = message.Version,
                Timestamp = DateTime.UtcNow,
                DataStandard = message.DataStandard,
                Products = message.Products,
                Filter = message.Filter,
                JobId = correlationId,
                Configuration = configuration
            };
    }
}
