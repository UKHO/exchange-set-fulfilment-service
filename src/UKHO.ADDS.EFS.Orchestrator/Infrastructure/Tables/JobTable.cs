using Azure.Data.Tables;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables
{
    internal class JobTable : StructuredTable<Job>
    {
        public JobTable(TableServiceClient tableServiceClient)
            : base(StorageConfiguration.JobTable, tableServiceClient, x => x.Id, x => x.Id)
        {
        }
    }
}
