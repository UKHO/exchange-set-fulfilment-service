using Polly;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;
using UKHO.ADDS.EFS.Builder.S100.Infrastructure;

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
            var configuration = context.Subject.Configuration;

            // Use the generic retry policy from HttpClientPolicyProvider
            var retryPolicy = HttpClientPolicyProvider.GetRetryPolicy<IResult<IBatchHandle>>(
                logger,
                r =>
                {
                    r.IsFailure(out var error, out var _);
                    return GetStatusCodeFromError(error);
                },
                configuration
            );

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
                    ReadUsers = new List<string> { "public" },
                    ReadGroups = new List<string> { "public" }
                },
                Attributes = new List<KeyValuePair<string, string>>
                {
                    new("Exchange Set Type", "Base"),
                    new("Frequency", "DAILY"),
                    new("Product Type", "S-100"),
                    new("Media Type", "Zip")
                },
                ExpiryDate = null
            };
        }

        private static int? GetStatusCodeFromError(IError error)
        {
            if (error != null)
                return Convert.ToInt32(error.Metadata["StatusCode"]);
            return null;
        }
    }
}
