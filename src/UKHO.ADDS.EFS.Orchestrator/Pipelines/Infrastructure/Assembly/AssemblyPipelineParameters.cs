using Microsoft.OpenApi.Extensions;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.VOS;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly
{
    internal class AssemblyPipelineParameters
    {
        public required int Version { get; init; }

        public required DateTime Timestamp { get; init; }

        public required DataStandard DataStandard { get; init; }

        public required string Products { get; init; }

        public required string Filter { get; init; }

        public required JobId JobId { get; init; }

        public required IConfiguration Configuration { get; init; }

        public Job CreateJob()
        {
            return new Job()
            {
                Id = JobId,
                Timestamp = Timestamp,
                DataStandard = DataStandard,
                RequestedProducts = Products,
                RequestedFilter = Filter,
                BatchId = BatchId.None
            };
        }

        public static AssemblyPipelineParameters CreateFrom(JobRequestApiMessage message, IConfiguration configuration, CorrelationId correlationId) =>
            new()
            {
                Version = message.Version,
                Timestamp = DateTime.UtcNow,
                DataStandard = message.DataStandard,
                Products = message.Products,
                Filter = message.Filter,
                JobId = VOS.JobId.From((string)correlationId),
                Configuration = configuration
            };
    }
}
