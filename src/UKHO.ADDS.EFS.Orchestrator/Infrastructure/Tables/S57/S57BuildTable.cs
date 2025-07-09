using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S57
{
    internal class S57BuildTable : BlobTable<S57Build>
    {
        public S57BuildTable(BlobServiceClient blobServiceClient)
            : base(StorageConfiguration.S57BuildContainer, blobServiceClient, x => x.JobId, x => x.JobId)
        {
        }
    }
}
