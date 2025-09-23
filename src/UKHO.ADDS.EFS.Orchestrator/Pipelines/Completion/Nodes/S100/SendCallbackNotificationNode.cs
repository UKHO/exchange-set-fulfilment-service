using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Factories;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S100
{
    internal class SendCallbackNotificationNode : CompletionPipelineNode<S100Build>
    {
        private readonly ICallbackNotificationService _callbackNotificationService;
        private readonly IExchangeSetResponseFactory _exchangeSetResponseFactory;

        public SendCallbackNotificationNode(
            CompletionNodeEnvironment nodeEnvironment,
            ICallbackNotificationService callbackNotificationService,
            IExchangeSetResponseFactory exchangeSetResponseFactory)
            : base(nodeEnvironment)
        {
            _callbackNotificationService = callbackNotificationService ?? throw new ArgumentNullException(nameof(callbackNotificationService));
            _exchangeSetResponseFactory = exchangeSetResponseFactory ?? throw new ArgumentNullException(nameof(exchangeSetResponseFactory));
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
                var callbackData = _exchangeSetResponseFactory.CreateResponse(job);
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
    }
}
