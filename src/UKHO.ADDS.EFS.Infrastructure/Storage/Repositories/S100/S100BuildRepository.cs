using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Infrastructure.Storage.Repositories.Implementation;

namespace UKHO.ADDS.EFS.Infrastructure.Storage.Repositories.S100
{
    internal class S100BuildRepository : BlobRepository<S100Build>
    {
        public S100BuildRepository(BlobServiceClient blobServiceClient)
            : base(StorageConfiguration.S100BuildContainer, blobServiceClient, x => (string)x.JobId, x => (string)x.JobId)
        {
        }
    }
}
