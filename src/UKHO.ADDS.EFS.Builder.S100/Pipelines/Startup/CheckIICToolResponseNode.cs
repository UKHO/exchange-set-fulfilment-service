using System.Net;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class CheckIICToolResponseNode : ExchangeSetPipelineNode
    {
        private const string IICToolBaseAddress = "http://localhost:8080";
        private const string WorkSpaceId = "working9";
        private const string ExchangeSetId = "es03";
        private const string Authkey = "D89D11D265B19CA5C2BE97A7FCB1EF21";


        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {

            await AddExchangeSet(context.Subject.WorkspaceRootPath);
            await AddContent(context.Subject.WorkspaceRootPath);
            return NodeResultStatus.Succeeded;
        }

        private static async Task AddExchangeSet(string workspaceRootPath)
        {
            string resourceLocation = $"{workspaceRootPath}/workspaces/{WorkSpaceId}/{ExchangeSetId}";
            if (!Directory.Exists(resourceLocation))
            {
                string path = $"/xchg-2.7/v2.7/addExchangeSet/{WorkSpaceId}/{ExchangeSetId}?authkey={Authkey}";

                using var client = new HttpClient { BaseAddress = new Uri(IICToolBaseAddress) };
                using var response = await client.GetAsync(path);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var bodyJson = await response.Content.ReadAsStringAsync();

                }
                response.EnsureSuccessStatusCode();
            }
        }

        private static async Task AddContent(string workspaceRootPath)
        {
            string resourceLocation = workspaceRootPath + "/spec-wise";

            if (Directory.Exists(resourceLocation))
            {
                var directories = Directory.GetDirectories(resourceLocation);

                foreach (var directory in directories)
                {
                    var directoryName = $"spec-wise/" + Path.GetFileName(directory);
                    string path = $"/xchg-2.7/v2.7/addContent/{WorkSpaceId}/{ExchangeSetId}?resourceLocation={directoryName}&authkey={Authkey}";

                    using var client = new HttpClient { BaseAddress = new Uri(IICToolBaseAddress) };
                    using var response = await client.GetAsync(path);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var bodyJson = await response.Content.ReadAsStringAsync();
                    }

                    response.EnsureSuccessStatusCode();
                }
            }
        }
    }
}
