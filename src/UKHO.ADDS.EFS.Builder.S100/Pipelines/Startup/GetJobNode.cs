using Serilog;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup.Logging;
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
                // Read DebugJob section from appsettings.development.json
                var debugJobConfig = context.Subject.Configuration.GetSection("DebugJob").Get<ExchangeSetJob>();

                // If DebugJob is null, use default values
                var debugJob = debugJobConfig ?? new ExchangeSetJob()
                {
                    Id = context.Subject.JobId,
                    DataStandard = ExchangeSetDataStandard.S100,
                    Timestamp = DateTime.UtcNow,
                    SalesCatalogueTimestamp = DateTime.UtcNow,
                    State = ExchangeSetJobState.InProgress,
                    Products = new List<S100Products>()
                    {
                        new S100Products
                        {
                            ProductName = "Product1",
                            LatestEditionNumber = 0,
                            LatestUpdateNumber = 1,
                            Status = new S100ProductStatus
                            {
                                StatusName = "newDataset",
                                StatusDate = DateTime.UtcNow
                            }
                        },
                        new S100Products
                        {
                            ProductName = "Product2",
                            LatestEditionNumber = 0,
                            LatestUpdateNumber = 1,
                            Status = new S100ProductStatus
                            {
                                StatusName = "newDataset",
                                StatusDate = DateTime.UtcNow.AddDays(-1)
                            }
                        },
                        new S100Products
                        {
                            ProductName = "Product3",
                            LatestEditionNumber = 0,
                            LatestUpdateNumber = 1,
                            Status = new S100ProductStatus
                            {
                                StatusName = "newDataset",
                                StatusDate = DateTime.UtcNow.AddDays(-7)
                            }
                        }
                    }
                };

                debugJob.Id = context.Subject.JobId;
                debugJob.CorrelationId = context.Subject.JobId;

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
            using var client = new HttpClient();
            client.BaseAddress = new Uri(baseAddress);

            using var response = await client.GetAsync(path);

            response.EnsureSuccessStatusCode();

            var jobJson = await response.Content.ReadAsStringAsync();
            var job = JsonCodec.Decode<ExchangeSetJob>(jobJson)!;

            context.Job = job;

            var logger = context.LoggerFactory.CreateLogger<GetJobNode>();
            logger.LogJobRetrieved(ExchangeSetJobLogView.CreateFromJob(job));
        }

        
    }
}
