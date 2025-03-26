using UKHO.ADDS.EFS.Builder.S100.Pipelines.Nodes;
using UKHO.ADDS.EFS.Common.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Common.Entities;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class GetRequestNode : BuilderNode<PipelineContext>
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext> context)
        {
            if (!string.Equals(context.Subject.RequestId, WellKnownRequestId.DebugRequestId))
            {
                await GetRequestAsync(context.Subject.BuildServiceEndpoint, $"/builds/{context.Subject.RequestId}/request", context.Subject);
            }

            return NodeResultStatus.Succeeded;
        }

        private static async Task GetRequestAsync(string baseAddress, string path, PipelineContext context)
        {
            using var client = new HttpClient { BaseAddress = new Uri(baseAddress) };
            using var response = await client.GetAsync(path);

            response.EnsureSuccessStatusCode();

            var requestJson = await response.Content.ReadAsStringAsync();
            var request = JsonCodec.Decode<ExchangeSetRequest>(requestJson)!;

            context.Request = request;
        }
    }
}
