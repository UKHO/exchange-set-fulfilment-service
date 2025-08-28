using Azure.Data.Tables;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Implementation;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables
{
    internal class BuildMementoTable : StructuredTable<BuildMemento>
    {
        public BuildMementoTable(TableServiceClient tableServiceClient)
            : base(StorageConfiguration.BuildMementoTable, tableServiceClient, x => (string)x.JobId, x => (string)x.JobId)
        {
        }
    }
}
