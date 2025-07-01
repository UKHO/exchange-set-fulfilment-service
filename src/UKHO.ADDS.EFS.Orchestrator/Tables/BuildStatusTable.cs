using Azure.Data.Tables;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Tables
{
    internal class BuildStatusTable : StructuredTable<BuildStatus>
    {
        public BuildStatusTable(TableServiceClient tableServiceClient)
            : base(StorageConfiguration.ExchangeSetBuildStatusTable, tableServiceClient, x => x.JobId, x => x.JobId)
        {
        }
    }
}
