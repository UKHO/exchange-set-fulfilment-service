using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Domain.Builds.S57;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Implementation;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S57
{
    internal class S57BuildRepository : BlobRepository<S57Build>
    {
        public S57BuildRepository(BlobServiceClient blobServiceClient)
            : base(StorageConfiguration.S57BuildContainer, blobServiceClient, x => (string)x.JobId, x => (string)x.JobId)
        {
        }
    }
}
