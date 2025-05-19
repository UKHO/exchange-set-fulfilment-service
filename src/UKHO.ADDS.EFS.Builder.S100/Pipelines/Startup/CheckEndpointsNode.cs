using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class CheckEndpointsNode : ExchangeSetPipelineNode
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            if (!(await context.Subject.ToolClient.PingAsync()).IsSuccess(out _))
            {
                return NodeResultStatus.Failed;
            }

            if (!(await context.Subject.ToolClient.ListWorkspaceAsync(context.Subject.WorkspaceAuthenticationKey)).IsSuccess(out _))
            {
                return NodeResultStatus.Failed;
            }

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
