using System.Text;
using UKHO.ADDS.EFS.Jobs.S57;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Builder.S57.Pipelines.Startup
{
    internal class GetJobNode : S57ExchangeSetPipelineNode
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S57ExchangeSetPipelineContext> context)
        {
            var blobClient = context.Subject.BlobClientFactory.CreateBlobClient(context.Subject.Configuration, $"{context.Subject.JobId}/{context.Subject.JobId}");

            var download = await blobClient.DownloadAsync();
            using var reader = new StreamReader(download.Value.Content, Encoding.UTF8);

            var blobJson = await reader.ReadToEndAsync();
            var job = JsonCodec.Decode<S57ExchangeSetJob>(blobJson)!;

            context.Subject.Job = job;
            return NodeResultStatus.Succeeded;
        }
    }
}
