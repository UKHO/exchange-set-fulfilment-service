using Azure.Data.Tables;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.NewEFS;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables
{
    internal class DataStandardTimestampTable : StructuredTable<DataStandardTimestamp>
    {
        public DataStandardTimestampTable(TableServiceClient tableServiceClient)
            : base(StorageConfiguration.DataStandardTimestampTable, tableServiceClient, x => x.DataStandard.ToString().ToLowerInvariant(), x => x.DataStandard.ToString().ToLowerInvariant())
        {
        }
    }
}
