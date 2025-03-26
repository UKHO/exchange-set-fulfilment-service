using Azure.Data.Tables;
using UKHO.ADDS.EFS.Common.Entities;
using UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Tables
{
    internal class ExchangeSetRequestTable : Table<ExchangeSetRequest>
    {
        public ExchangeSetRequestTable(TableServiceClient tableServiceClient)
            : base(tableServiceClient, x => x.Id, x => x.Id)
        {
        }
    }
}
