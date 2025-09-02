using Azure.Data.Tables;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Implementation;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables
{
    internal class DataStandardTimestampRepository : TableRepository<DataStandardTimestamp>
    {
        public DataStandardTimestampRepository(TableServiceClient tableServiceClient)
            : base(StorageConfiguration.DataStandardTimestampTable, tableServiceClient, x => x.DataStandard.ToString().ToLowerInvariant(), x => x.DataStandard.ToString().ToLowerInvariant())
        {
        }
    }
}
