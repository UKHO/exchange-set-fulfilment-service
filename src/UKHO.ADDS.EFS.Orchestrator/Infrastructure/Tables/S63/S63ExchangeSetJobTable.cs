using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Jobs.S63;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S63
{
    internal class S63ExchangeSetJobTable : BlobTable<S63ExchangeSetJob>
    {
        public S63ExchangeSetJobTable(BlobServiceClient blobServiceClient)
            : base(StorageConfiguration.S63JobContainer, blobServiceClient, x => x.Id, x => x.Id)
        {
        }
    }
}
