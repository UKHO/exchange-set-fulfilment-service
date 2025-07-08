using System.Text;
using UKHO.ADDS.EFS.NewEFS.S57;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Builder.S57.Pipelines.Startup
{
    internal class GetBuildNode : S57ExchangeSetPipelineNode
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S57ExchangeSetPipelineContext> context)
        {
            var blobClient = context.Subject.BlobClientFactory.CreateBlobClient(context.Subject.Configuration, $"{context.Subject.JobId}/{context.Subject.JobId}");

            var download = await blobClient.DownloadAsync();
            using var reader = new StreamReader(download.Value.Content, Encoding.UTF8);

            var blobJson = await reader.ReadToEndAsync();
            var build = JsonCodec.Decode<S57Build>(blobJson)!;

            context.Subject.Build = build;
            return NodeResultStatus.Succeeded;
        }
    }
}
