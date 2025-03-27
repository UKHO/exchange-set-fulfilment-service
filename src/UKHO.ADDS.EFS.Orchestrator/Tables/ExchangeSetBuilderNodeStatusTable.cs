using Azure.Data.Tables;
using UKHO.ADDS.EFS.Common.Entities;
using UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Tables
{
    internal class ExchangeSetBuilderNodeStatusTable : Table<ExchangeSetBuilderNodeStatus>
    {
        public ExchangeSetBuilderNodeStatusTable(TableServiceClient tableServiceClient)
            : base(tableServiceClient, x => x.RequestId, x => x.Timestamp)
        {
        }
    }
}
