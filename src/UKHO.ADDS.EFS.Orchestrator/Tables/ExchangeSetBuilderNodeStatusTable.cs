using Azure.Data.Tables;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Tables
{
    internal class ExchangeSetBuilderNodeStatusTable : StructuredTable<ExchangeSetBuilderNodeStatus>
    {
        public ExchangeSetBuilderNodeStatusTable(TableServiceClient tableServiceClient)
            : base("node-status-to-be-deleted", tableServiceClient, x => x.JobId, x => x.Sequence)
        {
        }
    }
}
