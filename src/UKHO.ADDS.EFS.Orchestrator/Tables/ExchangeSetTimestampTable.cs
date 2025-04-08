using Azure.Data.Tables;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Tables
{
    internal class ExchangeSetTimestampTable : Table<ExchangeSetTimestamp>
    {
        public ExchangeSetTimestampTable(TableServiceClient tableServiceClient)
            : base(tableServiceClient, x => x.DataStandard.ToString().ToLowerInvariant(), x => x.DataStandard.ToString().ToLowerInvariant())
        {
        }
    }
}
