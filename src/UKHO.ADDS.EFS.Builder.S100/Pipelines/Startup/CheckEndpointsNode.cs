using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class CheckEndpointsNode : ExchangeSetPipelineNode
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            await CheckEndpointAsync("http://localhost:8080", "/xchg-2.7/v2.7/dev?arg=test&authkey=noauth");
            await CheckEndpointAsync("http://localhost:8080", "/xchg-2.7/v2.7/listWorkspace?authkey=D89D11D265B19CA5C2BE97A7FCB1EF21");
            await CheckEndpointAsync(context.Subject.FileShareEndpoint, "health");

            return NodeResultStatus.Succeeded;
        }

        private static async Task CheckEndpointAsync(string baseAddress, string path)
        {
            using var client = new HttpClient { BaseAddress = new Uri(baseAddress) };
            using var response = await client.GetAsync(path);

            response.EnsureSuccessStatusCode();
        }
    }
}
