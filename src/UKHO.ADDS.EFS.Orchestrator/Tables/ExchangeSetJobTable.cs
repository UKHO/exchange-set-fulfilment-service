using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Tables
{
    public class ExchangeSetJobTable : BlobTable<ExchangeSetJob>
    {
        public ExchangeSetJobTable(BlobServiceClient blobServiceClient, ILogger<BlobTable<ExchangeSetJob>> logger)
            : base(blobServiceClient, x => x.Id, x => x.Id, logger)
        {
        }
    }
}
