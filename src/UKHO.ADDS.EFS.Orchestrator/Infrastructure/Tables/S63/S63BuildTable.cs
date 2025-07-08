using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.NewEFS.S63;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S63
{
    internal class S63BuildTable : BlobTable<S63Build>
    {
        public S63BuildTable(BlobServiceClient blobServiceClient)
            : base(StorageConfiguration.S63BuildContainer, blobServiceClient, x => x.JobId, x => x.JobId)
        {
        }
    }
}
