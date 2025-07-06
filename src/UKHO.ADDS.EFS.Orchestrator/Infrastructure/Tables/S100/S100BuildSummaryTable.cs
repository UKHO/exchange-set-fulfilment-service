using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S100
{
    internal class S100BuildSummaryTable : BlobTable<BuildSummary>
    {
        public S100BuildSummaryTable(BlobServiceClient blobServiceClient)
            : base(StorageConfiguration.S100JobContainer, blobServiceClient, x => x.JobId, x => $"{x.JobId}-summary")
        {
        }
    }
}
