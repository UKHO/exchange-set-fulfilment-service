using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100
{
    internal class CheckExchangeSetSizeExceeded : AssemblyPipelineNode<S100Build>
    {
        private readonly ILogger<GetS100ProductNamesNode> _logger;
        private const string MaxExchangeSetSizeInMBConfigKey = "orchestrator:Response:MaxExchangeSetSizeInMB";

        public CheckExchangeSetSizeExceeded(AssemblyNodeEnvironment nodeEnvironment, ILogger<GetS100ProductNamesNode> logger) : base(nodeEnvironment)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            return Task.FromResult(context.Subject.Job.JobState == JobState.Created && context.Subject.Job.RequestType != RequestType.Internal);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            var nodeResult = NodeResultStatus.NotRun;

            //if not B2C user then return noderesult ///TODO
            //return NodeResultStatus.Skipped;

            if (await IsExchangeSetSizeExceeded(context, job))
            {
                return NodeResultStatus.Failed;
            }
            return NodeResultStatus.Succeeded;
        }

        private async Task<bool> IsExchangeSetSizeExceeded(IExecutionContext<PipelineContext<S100Build>> context, Job job)
        {
            // Calculate total file size and check against the limit
            var maxExchangeSetSizeInMB = Environment.Configuration.GetValue<int>(MaxExchangeSetSizeInMBConfigKey);
            var totalFileSizeBytes = context.Subject.Build.ProductEditions.Sum(p => (long)p.FileSize);
            double bytesToKbFactor = 1024f;
            var totalFileSizeInMB = (totalFileSizeBytes / bytesToKbFactor) / bytesToKbFactor;

            if (totalFileSizeInMB > maxExchangeSetSizeInMB)
            {
                //_logger.LogExchangeSetSizeExceeded((long)totalFileSizeInMB, maxExchangeSetSizeInMB); ///TODO logging

                // Set up the error response for payload too large
                context.Subject.ErrorResponse = new ErrorResponseModel
                {
                    CorrelationId = job.Id.ToString(),
                    Errors =
                    [
                        new ErrorDetail
                        {
                             Source = "exchangeSetSize",
                             Description = "The Exchange Set requested is very large and will not be created, please use a standard Exchange Set provided by the UKHO."
                        }
                    ]
                };

                await context.Subject.SignalAssemblyError();

                return true;
            }

            return false;
        }
    }
}
