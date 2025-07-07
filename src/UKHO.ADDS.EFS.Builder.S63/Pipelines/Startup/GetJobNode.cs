using System.Text;
using UKHO.ADDS.EFS.Jobs.S63;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Builder.S63.Pipelines.Startup
{
    internal class GetJobNode : S63ExchangeSetPipelineNode
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S63ExchangeSetPipelineContext> context)
        {
            var blobClient = context.Subject.BlobClientFactory.CreateBlobClient(context.Subject.Configuration, $"{context.Subject.JobId}/{context.Subject.JobId}");

            var download = await blobClient.DownloadAsync();
            using var reader = new StreamReader(download.Value.Content, Encoding.UTF8);

            var blobJson = await reader.ReadToEndAsync();
            var job = JsonCodec.Decode<S63ExchangeSetJob>(blobJson)!;

            context.Subject.Job = job;
            return NodeResultStatus.Succeeded;
        }
    }
}
