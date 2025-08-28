using UKHO.ADDS.EFS.External;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Products;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly
{
    internal class AssemblyPipelineParameters
    {
        public MessageVersion Version { get; init; } = MessageVersion.From(1);

        public required DateTime Timestamp { get; init; }

        public required DataStandard DataStandard { get; init; }

        public required ProductNameList Products { get; init; }

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
                Timestamp = DateTime.UtcNow,
                DataStandard = message.DataStandard,
                Products = message.Products,
                Filter = message.Filter,
                JobId = EFS.Jobs.JobId.From((string)correlationId),
                Configuration = configuration
            };
    }
}
