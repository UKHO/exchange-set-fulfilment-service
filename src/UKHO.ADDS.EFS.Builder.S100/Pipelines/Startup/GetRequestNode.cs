using UKHO.ADDS.EFS.Common.Entities;
using UKHO.ADDS.EFS.Common.Messages;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class GetRequestNode : ExchangeSetPipelineNode
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            if (context.Subject.IsDebugSession)
            {
                // Create a debug request object (TODO - read from appsettings.development.json)
                // Send this back to the build API via the log context
                var request = new ExchangeSetRequest() { Id = context.Subject.RequestId, Message = new ExchangeSetRequestMessage() { DataStandard = ExchangeSetDataStandard.S100, Products = "example" } };
                context.Subject.Request = request;

                // Write back to API
                await context.Subject.NodeStatusWriter.WriteDebugExchangeSetRequest(request, context.Subject.BuildServiceEndpoint);
            }
            else
            {
                // Get the request data from the build API (not needed at the moment!)
                await GetRequestAsync(context.Subject.BuildServiceEndpoint, $"/builds/{context.Subject.RequestId}/request", context.Subject);
            }

            return NodeResultStatus.Succeeded;
        }

        private static async Task GetRequestAsync(string baseAddress, string path, ExchangeSetPipelineContext context)
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
