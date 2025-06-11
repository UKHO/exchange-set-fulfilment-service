using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    public class FileShareService: IFileShareService
    {
        private readonly IFileShareReadWriteClient _fileShareReadWriteClient;
        private readonly ILogger<FileShareService> _logger;
        private const string BusinessUnit = "ADDS-S100";
        private const string ProductType = "S-100";
        private const string ProductTypeQueryClause = $"$batch(ProductType) eq '{ProductType}' and ";
        private const int Limit = 100;
        private const int Start = 0;


        public FileShareService(IFileShareReadWriteClient fileShareReadWriteClient, ILogger<FileShareService> logger)
        {
            _fileShareReadWriteClient = fileShareReadWriteClient ?? throw new ArgumentNullException(nameof(fileShareReadWriteClient));
            _logger = logger;
        }

        public async Task<IResult<IBatchHandle>> CreateBatchAsync(string correlationId)
        {
            var createBatchResponseResult = await _fileShareReadWriteClient.CreateBatchAsync(GetBatchModel(), correlationId);

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

        public async Task<IResult<BatchSearchResponse>> SearchCommittedBatchesExcludingCurrentAsync(string currentBatchId, string correlationId)
        {
            var filter = $"BusinessUnit eq '{BusinessUnit}' and {ProductTypeQueryClause}$batch(BatchId) ne '{currentBatchId}'";
            
            var searchResult = await _fileShareReadWriteClient.SearchAsync(filter, Limit, Start, correlationId);

            if (searchResult.IsFailure(out var value, out var error))
            {
                
            }

            return searchResult;
        }

        public async Task<IResult<SetExpiryDateResponse>> SetExpiryDateAsync(List<BatchDetails> otherBatches, string correlationId, CancellationToken cancellationToken = default(CancellationToken))
        {
            IResult<SetExpiryDateResponse> lastResult = null;

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

                    lastResult = expiryResult;
                }
            }

            return lastResult;
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
