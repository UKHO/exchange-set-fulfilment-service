using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Jobs.S57;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S57
{
    internal class S57ExchangeSetJobTable : BlobTable<S57ExchangeSetJob>
    {
        public S57ExchangeSetJobTable(BlobServiceClient blobServiceClient)
            : base(StorageConfiguration.S57JobContainer, blobServiceClient, x => x.Id, x => x.Id)
        {
        }
    }
}
