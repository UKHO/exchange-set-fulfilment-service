using Azure.Data.Tables;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables
{
    internal class JobHistoryTable : StructuredTable<JobHistory>
    {
        // TODO Swap out for history interface

        public JobHistoryTable(TableServiceClient tableServiceClient)
            : base(StorageConfiguration.JobHistoryTable, tableServiceClient, x => x.Discriminant, x => x.Discriminant)
        {
        }
    }
}
