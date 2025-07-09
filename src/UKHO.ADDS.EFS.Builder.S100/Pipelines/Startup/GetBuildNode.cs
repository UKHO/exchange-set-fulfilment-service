using System.Text;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class GetBuildNode : S100ExchangeSetPipelineNode
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S100ExchangeSetPipelineContext> context)
        {
            var blobClient = context.Subject.BlobClientFactory.CreateBlobClient(context.Subject.Configuration, $"{context.Subject.JobId}/{context.Subject.JobId}");

            var download = await blobClient.DownloadAsync();
            using var reader = new StreamReader(download.Value.Content, Encoding.UTF8);

            var blobJson = await reader.ReadToEndAsync();
            var build = JsonCodec.Decode<S100Build>(blobJson)!;

            context.Subject.Build = build;
            return NodeResultStatus.Succeeded;
        }
    }
}
