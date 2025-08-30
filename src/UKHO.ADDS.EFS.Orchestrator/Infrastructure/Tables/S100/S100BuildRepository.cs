using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Implementation;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S100
{
    internal class S100BuildRepository : BlobRepository<S100Build>
    {
        public S100BuildRepository(BlobServiceClient blobServiceClient)
            : base(StorageConfiguration.S100BuildContainer, blobServiceClient, x => (string)x.JobId, x => (string)x.JobId)
        {
        }
    }
}
