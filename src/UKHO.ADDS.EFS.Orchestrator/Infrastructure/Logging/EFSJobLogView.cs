using UKHO.ADDS.EFS.NewEFS;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    internal class EFSJobLogView
    {
        public required string Id { get; init; }

        public required string? BatchId { get; init; }

        public DateTime Timestamp { get; init; }

        public JobState State { get; init; }

        public DataStandard DataStandard { get; init; }

        public static EFSJobLogView Create(Job job) =>
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
