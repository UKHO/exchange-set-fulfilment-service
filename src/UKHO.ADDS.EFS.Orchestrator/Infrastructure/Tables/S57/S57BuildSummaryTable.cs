using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S57
{
    internal class S57BuildSummaryTable : BlobTable<S57BuildSummary>
    {
        public S57BuildSummaryTable(BlobServiceClient blobServiceClient)
            : base(StorageConfiguration.S57JobContainer, blobServiceClient, x => x.JobId, x => x.SummaryId)
        {
        }
    }
}
