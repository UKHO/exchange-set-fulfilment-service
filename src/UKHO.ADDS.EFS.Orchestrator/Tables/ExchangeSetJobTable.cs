using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Tables
{
    internal class ExchangeSetJobTable : BlobTable<ExchangeSetJob>
    {
        public ExchangeSetJobTable(BlobServiceClient blobServiceClient)
            : base(blobServiceClient, x => "JobData", x => x.Id)
        {
        }
    }
}
