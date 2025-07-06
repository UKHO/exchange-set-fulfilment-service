using Azure.Data.Tables;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables
{
    internal class ExchangeSetJobTypeTable : StructuredTable<ExchangeSetJobType>
    {
        public ExchangeSetJobTypeTable(TableServiceClient tableServiceClient)
            : base(StorageConfiguration.ExchangeSetJobTypeTable, tableServiceClient, x => x.JobId, x => x.JobId)
        {
        }
    }
}
