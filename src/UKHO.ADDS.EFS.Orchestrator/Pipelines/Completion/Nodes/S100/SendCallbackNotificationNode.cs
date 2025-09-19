using UKHO.ADDS.Aspire.Configuration.Remote;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Domain.Services.Models;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S100
{
    internal class SendCallbackNotificationNode : CompletionPipelineNode<S100Build>
    {
        private readonly ICallbackNotificationService _callbackNotificationService;
        private readonly IExternalServiceRegistry _externalServiceRegistry;
        private readonly IFileNameGeneratorService _fileNameGeneratorService;
        private readonly IConfiguration _configuration;

        public SendCallbackNotificationNode(
            CompletionNodeEnvironment nodeEnvironment,
            ICallbackNotificationService callbackNotificationService,
            IExternalServiceRegistry externalServiceRegistry,
            IFileNameGeneratorService fileNameGeneratorService,
            IConfiguration configuration)
            : base(nodeEnvironment)
        {
            _callbackNotificationService = callbackNotificationService;
            _externalServiceRegistry = externalServiceRegistry;
            _fileNameGeneratorService = fileNameGeneratorService;
            _configuration = configuration;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            // Only execute if there's a callback URI and the batch was successfully committed
            return Task.FromResult(
                context.Subject.Job.CallbackUri != CallbackUri.None && 
                context.Subject.Job.BatchId != BatchId.None &&
                Environment.BuilderExitCode == BuilderExitCode.Success);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job!;

            try
            {
                var callbackData = CreateCallbackData(job);
                await _callbackNotificationService.SendCallbackNotificationAsync(job, callbackData, Environment.CancellationToken);
                
                return NodeResultStatus.Succeeded;
            }
            catch (Exception)
            {
                // Don't fail the entire pipeline if callback notification fails
                // The exchange set has been successfully created, callback is supplementary
                return NodeResultStatus.SucceededWithErrors;
            }
        }

        private CallbackExchangeSetData CreateCallbackData(Job job)
        {
            var fssEndpoint = _externalServiceRegistry.GetServiceEndpoint(ProcessNames.FileShareService);
            var baseUri = fssEndpoint.Uri.ToString().TrimEnd('/');
            
            // Get the exchange set name template from configuration
            var exchangeSetNameTemplate = _configuration["orchestrator:Builders:S100:ExchangeSetNameTemplate"] ?? "S100-ExchangeSet_{0}.zip";
            var fileName = _fileNameGeneratorService.GenerateFileName(exchangeSetNameTemplate, job.Id);

            return new CallbackExchangeSetData
            {
                Links = new CallbackLinks
                {
                    ExchangeSetBatchStatusUri = new CallbackLink { Href = $"{baseUri}/batch/{job.BatchId}/status" },
                    ExchangeSetBatchDetailsUri = new CallbackLink { Href = $"{baseUri}/batch/{job.BatchId}" },
                    ExchangeSetFileUri = new CallbackLink { Href = $"{baseUri}/batch/{job.BatchId}/files/{fileName}" },
                    AioExchangeSetFileUri = new CallbackLink { Href = $"{baseUri}/batch/{job.BatchId}/files/aio123.zip" },
                    ErrorFileUri = new CallbackLink { Href = $"{baseUri}/batch/{job.BatchId}/files/error.txt" }
                },
                ExchangeSetUrlExpiryDateTime = job.ExchangeSetUrlExpiryDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                RequestedProductCount = (int)job.RequestedProductCount,
                ExchangeSetCellCount = (int)job.ExchangeSetProductCount,
                RequestedProductsAlreadyUpToDateCount = (int)job.RequestedProductsAlreadyUpToDateCount,
                RequestedAioProductCount = 2, // Static value as per requirement
                AioExchangeSetCellCount = 1, // Static value as per requirement
                RequestedAioProductsAlreadyUpToDateCount = 1, // Static value as per requirement
                RequestedProductsNotInExchangeSet = job.RequestedProductsNotInExchangeSet
                    .Select(p => new CallbackMissingProduct 
                    { 
                        ProductName = p.ProductName.ToString(), 
                        Reason = p.Reason.ToString().ToLower() 
                    }).ToList(),
                FssBatchId = job.BatchId.ToString()
            };
        }
    }
}
