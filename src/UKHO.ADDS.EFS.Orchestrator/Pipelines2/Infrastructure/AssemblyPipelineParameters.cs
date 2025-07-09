using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure
{
    internal class AssemblyPipelineParameters
    {
        public required int Version { get; init; }

        public required DateTime Timestamp { get; init; }

        public required DataStandard DataStandard { get; init; }

        public required string Products { get; init; }

        public required string Filter { get; init; }

        public required string JobId { get; init; }

        public required IConfiguration Configuration { get; init; }

        public Job CreateJob()
        {
            return new Job()
            {
                Id = JobId,
                Timestamp = Timestamp,
                DataStandard = DataStandard,
                RequestedProducts = Products,
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

        public static AssemblyPipelineParameters CreateFrom(JobRequestQueueMessage message, IConfiguration configuration) =>
            new()
            {
                Version = message.Version,
                Timestamp = message.Timestamp,
                DataStandard = message.DataStandard,
                Products = message.Products,
                Filter = message.Filter,
                JobId = message.CorrelationId,
                Configuration = configuration
            };
    }
}
