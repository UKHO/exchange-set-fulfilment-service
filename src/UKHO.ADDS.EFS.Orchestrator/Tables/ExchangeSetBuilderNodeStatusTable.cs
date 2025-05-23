﻿using Azure.Data.Tables;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Tables
{
    internal class ExchangeSetBuilderNodeStatusTable : StructuredTable<ExchangeSetBuilderNodeStatus>
    {
        public ExchangeSetBuilderNodeStatusTable(TableServiceClient tableServiceClient)
            : base(tableServiceClient, x => x.JobId, x => x.Sequence)
        {
        }
    }
}
