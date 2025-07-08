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
    }
}
