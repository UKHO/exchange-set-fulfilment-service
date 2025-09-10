using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    internal class JobLogView
    {
        public required JobId Id { get; init; }

        public required BatchId BatchId { get; init; }

        public DateTime Timestamp { get; init; }

        public JobState State { get; init; }

        public DataStandard DataStandard { get; init; }

        public static JobLogView Create(Job job) =>
            new()
            {
                Id = job.Id,
                BatchId = job.BatchId,
                Timestamp = job.Timestamp,
                State = job.JobState,
                DataStandard = job.DataStandard,
            };
    }
}
