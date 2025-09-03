using Azure.Data.Tables;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Infrastructure.Storage.Repositories.Implementation;

namespace UKHO.ADDS.EFS.Infrastructure.Storage.Repositories
{
    internal class DataStandardTimestampRepository : TableRepository<DataStandardTimestamp>
    {
        public DataStandardTimestampRepository(TableServiceClient tableServiceClient)
            : base(StorageConfiguration.DataStandardTimestampTable, tableServiceClient, x => x.DataStandard.ToString().ToLowerInvariant(), x => x.DataStandard.ToString().ToLowerInvariant())
        {
        }
    }
}
