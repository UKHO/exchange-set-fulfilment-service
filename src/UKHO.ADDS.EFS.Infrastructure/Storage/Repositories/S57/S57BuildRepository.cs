using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Domain.Builds.S57;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Infrastructure.Storage.Repositories.Implementation;

namespace UKHO.ADDS.EFS.Infrastructure.Storage.Repositories.S57
{
    internal class S57BuildRepository : BlobRepository<S57Build>
    {
        public S57BuildRepository(BlobServiceClient blobServiceClient)
            : base(StorageConfiguration.S57BuildContainer, blobServiceClient, x => (string)x.JobId, x => (string)x.JobId)
        {
        }
    }
}
