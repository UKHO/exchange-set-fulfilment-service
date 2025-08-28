using Azure.Data.Tables;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Implementation;
using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables
{
    internal class JobTable : StructuredTable<Job>
    {
        public JobTable(TableServiceClient tableServiceClient)
            : base(StorageConfiguration.JobTable, tableServiceClient, x => (string)x.Id, x => (string)x.Id)
        {
        }
    }
}
