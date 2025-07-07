using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S63
{
    internal class S63BuildSummaryTable : BlobTable<BuildSummary>
    {
        public S63BuildSummaryTable(BlobServiceClient blobServiceClient)
            : base(StorageConfiguration.S63JobContainer, blobServiceClient, x => x.JobId, x => $"{x.JobId}-summary")
        {
        }
    }
}
