using Serilog;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class CheckEndpointsNode : ExchangeSetPipelineNode
    {
        private readonly IHttpClientFactory _clientFactory;

        public CheckEndpointsNode(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
#pragma warning disable LOG001
            Log.Information($"FS ENDPOINT IS {context.Subject.FileShareEndpoint}");
#pragma warning restore LOG001

            if (!(await context.Subject.ToolClient.PingAsync()).IsSuccess(out _))
            {
                throw new InvalidOperationException("IIC Ping failed");
            }

            if (!(await context.Subject.ToolClient.ListWorkspaceAsync(context.Subject.WorkspaceAuthenticationKey)).IsSuccess(out _))
            {
                throw new InvalidOperationException("IIC ListWorkspaces");
            }

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
