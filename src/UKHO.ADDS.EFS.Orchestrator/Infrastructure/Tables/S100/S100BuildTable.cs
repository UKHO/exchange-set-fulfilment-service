using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Implementation;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S100
{
    internal class S100BuildTable : BlobTable<S100Build>
    {
        public S100BuildTable(BlobServiceClient blobServiceClient)
            : base(StorageConfiguration.S100BuildContainer, blobServiceClient, x => (string)x.JobId, x => (string)x.JobId)
        {
        }
    }
}
