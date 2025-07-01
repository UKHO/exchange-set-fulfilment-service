using System.Text;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup.Logging;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class GetJobNode : ExchangeSetPipelineNode
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var blobClient = context.Subject.BlobClientFactory.CreateBlobClient(context.Subject.Configuration, $"{context.Subject.JobId}/{context.Subject.JobId}");

            var download = await blobClient.DownloadAsync();
            using var reader = new StreamReader(download.Value.Content, Encoding.UTF8);

            var blobJson = await reader.ReadToEndAsync();
            var job = JsonCodec.Decode<S100ExchangeSetJob>(blobJson)!;

            context.Subject.Job = job;

            return NodeResultStatus.Succeeded;
        }
    }
}
