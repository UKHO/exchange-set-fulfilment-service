using Azure.Data.Tables;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Implementation;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables
{
    internal class BuildMementoRepository : TableRepository<BuildMemento>
    {
        public BuildMementoRepository(TableServiceClient tableServiceClient)
            : base(StorageConfiguration.BuildMementoRepositoryName, tableServiceClient, x => (string)x.JobId, x => (string)x.JobId)
        {
        }
    }
}
