using Azure.Data.Tables;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables
{
    internal class ExchangeSetTimestampTable : StructuredTable<ExchangeSetTimestamp>
    {
        public ExchangeSetTimestampTable(TableServiceClient tableServiceClient)
            : base(StorageConfiguration.ExchangeSetTimestampTable, tableServiceClient, x => x.DataStandard.ToString().ToLowerInvariant(), x => x.DataStandard.ToString().ToLowerInvariant())
        {
        }

        public async Task<DateTime> GetTimestampForJobAsync(ExchangeSetJob job)
        {
            var timestamp = DateTime.MinValue;
            var timestampKey = job.DataStandard.ToString().ToLowerInvariant();

            var timestampResult = await GetAsync(timestampKey, timestampKey);

            if (timestampResult.IsSuccess(out var timestampEntity))
            {
                if (timestampEntity.Timestamp.HasValue)
                {
                    timestamp = timestampEntity.Timestamp!.Value;
                }
            }

            return timestamp;
        }
    }
}
