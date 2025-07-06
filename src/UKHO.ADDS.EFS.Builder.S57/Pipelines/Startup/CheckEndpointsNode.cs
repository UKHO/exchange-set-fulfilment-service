using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S57.Pipelines.Startup
{
    internal class CheckEndpointsNode : S57ExchangeSetPipelineNode
    {
        private readonly IHttpClientFactory _clientFactory;

        public CheckEndpointsNode(IHttpClientFactory clientFactory) => _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S57ExchangeSetPipelineContext> context)
        {
            await CheckEndpointAsync(context.Subject.FileShareEndpoint, "health");

            return NodeResultStatus.Succeeded;
        }

        private async Task CheckEndpointAsync(string baseAddress, string path)
        {
            var client = _clientFactory.CreateClient();
            client.BaseAddress = new Uri(baseAddress);

            using var response = await client.GetAsync(path);
            response.EnsureSuccessStatusCode();
        }
    }
}
