using Azure.Data.Tables;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Tables
{
    internal class ExchangeSetJobTable : Table<ExchangeSetJob>
    {
        public ExchangeSetJobTable(TableServiceClient tableServiceClient)
            : base(tableServiceClient, x => x.Id, x => x.Id)
        {
        }
    }
}
