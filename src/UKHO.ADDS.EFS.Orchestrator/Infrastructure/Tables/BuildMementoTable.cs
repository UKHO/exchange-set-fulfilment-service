using Azure.Data.Tables;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Implementation;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables
{
    internal class BuildMementoTable : StructuredTable<BuildMemento>
    {
        public BuildMementoTable(TableServiceClient tableServiceClient)
            : base(StorageConfiguration.BuildMementoTable, tableServiceClient, x => x.JobId, x => x.JobId)
        {
        }
    }
}
