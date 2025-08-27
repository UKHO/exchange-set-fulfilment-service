using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    internal class EFSJobLogView
    {
        public required JobId Id { get; init; }

        public required BatchId BatchId { get; init; }

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
