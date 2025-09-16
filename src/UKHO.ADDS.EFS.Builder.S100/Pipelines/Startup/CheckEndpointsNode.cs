﻿using Serilog;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class CheckEndpointsNode : S100ExchangeSetPipelineNode
    {
        private readonly IHttpClientFactory _clientFactory;

        public CheckEndpointsNode(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S100ExchangeSetPipelineContext> context)
        {
            //if (!(await context.Subject.ToolClient.ListWorkspaceAsync(context.Subject.WorkspaceAuthenticationKey)).IsSuccess(out _))
            //{
            //    return NodeResultStatus.Failed;
            //}
            // Rhz: replace with.
            var myresult = await context.Subject.ToolClient.ListWorkspaceAsync(context.Subject.WorkspaceAuthenticationKey);
            if (!myresult.IsSuccess(out var response))
            {
                Log.Information($"Check Endpoint node failed with response:{response}");
                myresult.Errors.ToList().ForEach(e => Log.Information($"Check Endpoint node failed with error:{e}"));
                return NodeResultStatus.Failed;
            }
            // Rhz: end replace.

            await CheckEndpointAsync(context.Subject.FileShareHealthEndpoint);

            return NodeResultStatus.Succeeded;
        }

        private async Task CheckEndpointAsync(string endpoint)
        {
            var client = _clientFactory.CreateClient();

            using var response = await client.GetAsync(endpoint);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
#pragma warning disable LOG001
                Log.Information($"****** HEALTH {endpoint} FAILED (CHECK)");
#pragma warning restore LOG001

            }
        }
    }
}
