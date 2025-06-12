using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.EFS.Domain.RetryPolicy;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble
{
    internal class CreateBatchNode : ExchangeSetPipelineNode
    {
        private readonly IFileShareReadWriteClient _fileShareReadWriteClient;

        public CreateBatchNode(IFileShareReadWriteClient fileShareReadWriteClient) : base()
        {
            _fileShareReadWriteClient = fileShareReadWriteClient ?? throw new ArgumentNullException(nameof(fileShareReadWriteClient));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(
            IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var logger = context.Subject.LoggerFactory.CreateLogger<CreateBatchNode>();

            // Use the generic retry policy from HttpClientPolicyProvider (moved logic)
            var retryPolicy = HttpClientPolicyProvider.GetGenericResultRetryPolicy<IBatchHandle>(logger);

            var result = await retryPolicy.ExecuteAsync(async () =>
                await _fileShareReadWriteClient.CreateBatchAsync(GetBatchModel(), context.Subject.Job.CorrelationId));

            if (result.IsSuccess(out var handle, out var error))
            {
                context.Subject.BatchId = handle.BatchId;
                return NodeResultStatus.Succeeded;
            }
            logger.LogCreateBatchNodeFailed(error);
            return NodeResultStatus.Failed;
        }

        private static BatchModel GetBatchModel()
        {
            return new BatchModel
            {
                BusinessUnit = "ADDS-S100",
                Acl = new Acl
                {
                    ReadUsers = ["public"],
                    ReadGroups = ["public"]
                },
                Attributes =
                [
                    new("Exchange Set Type", "Base"),
                    new("Frequency", "DAILY"),
                    new("Product Type", "S-100"),
                    new("Media Type", "Zip")
                ],
                ExpiryDate = null
            };
        }
    }
}
