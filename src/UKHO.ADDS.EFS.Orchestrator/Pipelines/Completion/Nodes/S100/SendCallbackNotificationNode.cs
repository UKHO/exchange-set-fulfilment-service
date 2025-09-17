using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Files;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api.Models;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S100
{
    /// <summary>
    /// Pipeline node for sending callback notifications after successful file share batch commit
    /// </summary>
    internal class SendCallbackNotificationNode : CompletionPipelineNode<S100Build>
    {
        private readonly ICallbackService _callbackService;

        public SendCallbackNotificationNode(CompletionNodeEnvironment nodeEnvironment, ICallbackService callbackService)
            : base(nodeEnvironment)
        {
            _callbackService = callbackService;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            // Only execute if:
            // 1. A callback URI was provided
            // 2. The file share batch was successfully committed
            return Task.FromResult(
                context.Subject.Job.CallbackUri != CallbackUri.None && 
                Environment.BuilderExitCode == Domain.Builds.BuilderExitCode.Success);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;
            var correlationId = job.GetCorrelationId();

            try
            {
                var responseData = CreateExchangeSetResponse(context);
                
                var success = await _callbackService.SendCallbackAsync(
                    job.CallbackUri, 
                    responseData, 
                    correlationId, 
                    Environment.CancellationToken);

                if (success)
                {
                    Environment.Logger.LogCallbackNotificationSent(job.Id.ToString(), job.CallbackUri.Value, correlationId.ToString());
                    return NodeResultStatus.Succeeded;
                }
                else
                {
                    Environment.Logger.LogCallbackNotificationFailed(job.Id.ToString(), job.CallbackUri.Value, correlationId.ToString());
                    // Return success even if callback fails - the main operation was successful
                    return NodeResultStatus.Succeeded;
                }
            }
            catch (Exception ex)
            {
                Environment.Logger.LogCallbackNotificationFailed(job.Id.ToString(), job.CallbackUri.Value, correlationId.ToString());
                // Return success even if callback fails - the main operation was successful
                return NodeResultStatus.Succeeded;
            }
        }

        private CustomExchangeSetResponse CreateExchangeSetResponse(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;

            var links = new ExchangeSetLinks
            {
                ExchangeSetBatchStatusUri = new Link { Uri = new Uri($"http://fss.ukho.gov.uk/batch/{job.BatchId}/status") },
                ExchangeSetBatchDetailsUri = new Link { Uri = new Uri($"http://fss.ukho.gov.uk/batch/{job.BatchId}") },
                ExchangeSetFileUri = new Link { Uri = new Uri($"http://fss.ukho.gov.uk/batch/{job.BatchId}/files/exchangeset123.zip") },
                AioExchangeSetFileUri = new Link { Uri = new Uri($"http://fss.ukho.gov.uk/batch/{job.BatchId}/files/aio123.zip") },
                ErrorFileUri = new Link { Uri = new Uri($"http://fss.ukho.gov.uk/batch/{job.BatchId}/files/error.txt") }
            };

            return new CustomExchangeSetResponse
            {
                Links = links,
                ExchangeSetUrlExpiryDateTime = job.ExchangeSetUrlExpiryDateTime,
                RequestedProductCount = job.RequestedProductCount,
                ExchangeSetProductCount = job.ExchangeSetProductCount,
                RequestedProductsAlreadyUpToDateCount = job.RequestedProductsAlreadyUpToDateCount,
                RequestedAioProductCount = Domain.Products.ProductCount.From(2), // Mock value as per acceptance criteria
                AioExchangeSetCellCount = Domain.Products.ProductCount.From(1), // Mock value as per acceptance criteria
                RequestedAioProductsAlreadyUpToDateCount = Domain.Products.ProductCount.From(1), // Mock value as per acceptance criteria
                RequestedProductsNotInExchangeSet = job.RequestedProductsNotInExchangeSet,
                FssBatchId = job.BatchId
            };
        }
    }
}
