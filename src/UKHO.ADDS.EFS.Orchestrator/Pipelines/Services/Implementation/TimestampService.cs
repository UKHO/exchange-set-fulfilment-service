using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services.Storage;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Services.Implementation
{
    internal class TimestampService : ITimestampService
    {
        private readonly IRepository<DataStandardTimestamp> _timestampRepository;

        public TimestampService(IRepository<DataStandardTimestamp> timestampRepository)
        {
            _timestampRepository = timestampRepository;
        }

        public async Task<DateTime> GetTimestampForJobAsync(Job job)
        {
            var timestamp = DateTime.MinValue;
            var timestampKey = job.DataStandard.ToString().ToLowerInvariant();

            var timestampResult = await _timestampRepository.GetUniqueAsync(timestampKey);

            if (timestampResult.IsSuccess(out var timestampEntity))
            {
                if (timestampEntity.Timestamp.HasValue)
                {
                    timestamp = timestampEntity.Timestamp!.Value;
                }
            }

            return timestamp;
        }

        public async Task SetTimestampForJobAsync(Job job)
        {
            var updateTimestampEntity = new DataStandardTimestamp { DataStandard = job.DataStandard, Timestamp = job.DataStandardTimestamp };
            await _timestampRepository.UpsertAsync(updateTimestampEntity);
        }
    }
}
