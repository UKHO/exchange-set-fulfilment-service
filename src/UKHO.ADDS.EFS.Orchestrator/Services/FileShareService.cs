using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    public class FileShareService: IFileShareService
    {
        private readonly IFileShareReadWriteClient _fileShareReadWriteClient;

        public FileShareService(IFileShareReadWriteClient fileShareReadWriteClient)
        {
            _fileShareReadWriteClient = fileShareReadWriteClient ?? throw new ArgumentNullException(nameof(fileShareReadWriteClient));
        }

        public async Task<IResult<IBatchHandle>> CreateBatchAsync(ExchangeSetRequestQueueMessage queueMessage)
        {
            var createBatchResponseResult = await _fileShareReadWriteClient.CreateBatchAsync(GetBatchModel(), queueMessage.CorrelationId);

            if (createBatchResponseResult.IsFailure(out var commitError, out _))
            {

            }

            return createBatchResponseResult;
        }

        public async Task<IResult<CommitBatchResponse>> CommitBatchAsync(string batchId, string correlationId, CancellationToken cancellationToken)
        {
            var commitBatchResult = await _fileShareReadWriteClient.CommitBatchAsync(new BatchHandle(batchId), correlationId, CancellationToken.None);

            if (commitBatchResult.IsFailure(out var commitError, out _))
            {

            }

            return commitBatchResult;
        }

        public async Task<IResult<BatchSearchResponse>> SearchAllCommitBatchesAsync(string currentBatchId, string correlationId)
        {
            var filter =
                $"BusinessUnit eq 'ADDS-S100' and " +
                $"$batch(ProductType) eq 'S-100' and " +
                $"$batch(BatchId) ne '{currentBatchId}'";

            var limit = 100;
            var start = 0;
            
            var searchResult = await _fileShareReadWriteClient.SearchAsync(filter, limit, start, correlationId);

            if (searchResult.IsFailure(out var value, out var error))
            {
                
            }

            return searchResult;
        }

        public async Task<IResult<SetExpiryDateResponse>> SetExpiryDateAsync(List<BatchDetails> otherBatches, string correlationId, CancellationToken cancellationToken = default(CancellationToken))
        {
            IResult<SetExpiryDateResponse> result = null;

            foreach (var batch in otherBatches)
            {
                if (!string.IsNullOrEmpty(batch.BatchId))
                {
                    var expiryResult = await _fileShareReadWriteClient.SetExpiryDateAsync(
                        batch.BatchId,
                        new BatchExpiryModel { ExpiryDate = DateTime.UtcNow },
                        correlationId,
                        CancellationToken.None);

                    if (expiryResult.IsFailure(out var expiryError, out _))
                    {
                        return expiryResult;
                    }
                    result = expiryResult;
                }
            }

            return result;
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
    }
}
