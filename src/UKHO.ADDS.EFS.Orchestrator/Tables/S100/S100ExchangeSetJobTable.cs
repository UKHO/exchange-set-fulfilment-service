using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Tables.S100
{
    internal class S100ExchangeSetJobTable : BlobTable<S100ExchangeSetJob>
    {
        public S100ExchangeSetJobTable(BlobServiceClient blobServiceClient)
            : base(StorageConfiguration.S100JobContainer, blobServiceClient, x => x.Id, x => x.Id)
        {
        }
    }
}
