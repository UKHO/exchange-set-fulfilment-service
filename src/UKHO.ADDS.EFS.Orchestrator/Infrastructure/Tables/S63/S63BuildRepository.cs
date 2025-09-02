using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Domain.Builds.S63;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Implementation;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S63
{
    internal class S63BuildRepository : BlobRepository<S63Build>
    {
        public S63BuildRepository(BlobServiceClient blobServiceClient)
            : base(StorageConfiguration.S63BuildContainer, blobServiceClient, x => (string)x.JobId, x => (string)x.JobId)
        {
        }
    }
}
