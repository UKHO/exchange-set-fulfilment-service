using Serilog;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Entities;
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
            if (context.Subject.IsDebugSession)
            {
                // Create a debug job (TODO - read example values from appsettings.development.json)
                // Send this back to the build API via the log context
                var debugJob = new ExchangeSetJob() { Id = context.Subject.JobId, DataStandard = ExchangeSetDataStandard.S100, Timestamp = DateTime.UtcNow, SalesCatalogueTimestamp = DateTime.UtcNow, State = ExchangeSetJobState.InProgress, Products = new List<S100Products>()};
                context.Subject.Job = debugJob;

                // Write back to API
                await context.Subject.NodeStatusWriter.WriteDebugExchangeSetJob(debugJob, context.Subject.BuildServiceEndpoint);
            }
            else
            {
                // Get the job from the build API
                await GetJobAsync(context.Subject.BuildServiceEndpoint, $"/jobs/{context.Subject.JobId}", context.Subject);
            }

            return NodeResultStatus.Succeeded;
        }

        private static async Task GetJobAsync(string baseAddress, string path, ExchangeSetPipelineContext context)
        {
            using var client = new HttpClient { BaseAddress = new Uri(baseAddress) };
            using var response = await client.GetAsync(path);

            response.EnsureSuccessStatusCode();

            var jobJson = await response.Content.ReadAsStringAsync();
            var job = JsonCodec.Decode<ExchangeSetJob>(jobJson)!;

            context.Job = job;

            Log.Information($"Received job : {jobJson}");
        }
    }
}
