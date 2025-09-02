using Azure.Data.Tables;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Implementation;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables
{
    internal class JobRepository : TableRepository<Job>
    {
        public JobRepository(TableServiceClient tableServiceClient)
            : base(StorageConfiguration.JobRepositoryName, tableServiceClient, x => (string)x.Id, x => (string)x.Id)
        {
        }
    }
}
