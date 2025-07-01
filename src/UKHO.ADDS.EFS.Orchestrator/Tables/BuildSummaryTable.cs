using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Tables
{
    internal class BuildSummaryTable : BlobTable<BuildSummary>
    {
        public BuildSummaryTable(BlobServiceClient blobServiceClient)
            : base(StorageConfiguration.S100JobContainer, blobServiceClient, x => x.JobId, x => $"{x.JobId}-summary")
        {
        }
    }
}
