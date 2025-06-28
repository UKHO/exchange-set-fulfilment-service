using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup.Logging;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class GetJobNode : ExchangeSetPipelineNode
    {
        private readonly IHttpClientFactory _clientFactory;

        public GetJobNode(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        }
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            if (context.Subject.IsDebugSession)
            {
                // Read DebugJob section from appsettings.development.json
                var debugJobConfig = context.Subject.Configuration.GetSection("DebugJob").Get<S100ExchangeSetJob>();

                // If DebugJob is null, use default values
                var debugJob = debugJobConfig ?? new S100ExchangeSetJob()
                {
                    Id = context.Subject.JobId,
                    BatchId = context.Subject.BatchId,
                    DataStandard = ExchangeSetDataStandard.S100,
                    Timestamp = DateTime.UtcNow,
                    SalesCatalogueTimestamp = DateTime.UtcNow,
                    State = ExchangeSetJobState.InProgress,
                    Products = new List<S100Products>()
                    {
                        new S100Products
                        {
                            ProductName = "101GB00479ABCDEFG",
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
                            ProductName = "101FR00479ABCDXYZ",
                            LatestEditionNumber = 0,
                            LatestUpdateNumber = 0,
                            Status = new S100ProductStatus
                            {
                                StatusName = "newDataset",
                                StatusDate = DateTime.UtcNow
                            }
                        },
                        new S100Products
                        {
                            ProductName = "101FR004791234567",
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
                            ProductName = "104GB00_479ABCDEFG",
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
                            ProductName = "104FR00479ABCDXYZ",
                            LatestEditionNumber = 0,
                            LatestUpdateNumber = 1,
                            Status = new S100ProductStatus
                            {
                                StatusName = "newDataset",
                                StatusDate = DateTime.UtcNow.AddDays(-7)
                            }
                        },
                        new S100Products
                        {
                            ProductName = "111CA00_3456tyu_DEFG",
                            LatestEditionNumber = 0,
                            LatestUpdateNumber = 1,
                            Status = new S100ProductStatus
                            {
                                StatusName = "newDataset",
                                StatusDate = DateTime.UtcNow.AddDays(-7)
                            }
                        },
                        new S100Products
                        {
                            ProductName = "111FR00_t6yhgtu_YZ",
                            LatestEditionNumber = 0,
                            LatestUpdateNumber = 1,
                            Status = new S100ProductStatus
                            {
                                StatusName = "newDataset",
                                StatusDate = DateTime.UtcNow.AddDays(-7)
                            }
                        },
                        new S100Products
                        {
                            ProductName = "102GB00_fgbhty78_edfr",
                            LatestEditionNumber = 0,
                            LatestUpdateNumber = 1,
                            Status = new S100ProductStatus
                            {
                                StatusName = "newDataset",
                                StatusDate = DateTime.UtcNow.AddDays(-7)
                            }
                        },
                        new S100Products
                        {
                            ProductName = "102GB01_qwser_ZZ",
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
        private async Task GetJobAsync(string baseAddress, string path, ExchangeSetPipelineContext context)
        {
            var client = _clientFactory.CreateClient();
            client.BaseAddress = new Uri(baseAddress);

            using var response = await client.GetAsync(path);

            response.EnsureSuccessStatusCode();

            var jobJson = await response.Content.ReadAsStringAsync();
            var job = JsonCodec.Decode<S100ExchangeSetJob>(jobJson)!;

            context.Job = job;

            var logger = context.LoggerFactory.CreateLogger<GetJobNode>();
            logger.LogJobRetrieved(ExchangeSetJobLogView.CreateFromJob(job));
        }
    }
}
