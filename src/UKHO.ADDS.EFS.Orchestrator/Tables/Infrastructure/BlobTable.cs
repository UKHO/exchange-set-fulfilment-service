using System.Text;
using Azure.Storage.Blobs;
using UKHO.ADDS.Infrastructure.Results;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure
{
    internal abstract class BlobTable<TEntity> : ITable<TEntity> where TEntity : class
    {
        private readonly BlobServiceClient _blobClient;

        private readonly Func<TEntity, string> _partitionKeySelector;
        private readonly Func<TEntity, string> _rowKeySelector;
        private readonly ILogger<BlobTable<TEntity>> _logger;

        protected BlobTable(BlobServiceClient blobServiceClient, Func<TEntity, string> partitionKeySelector, Func<TEntity, string> rowKeySelector, ILogger<BlobTable<TEntity>> logger)
        {
            _partitionKeySelector = partitionKeySelector ?? throw new ArgumentNullException(nameof(partitionKeySelector));
            _rowKeySelector = rowKeySelector ?? throw new ArgumentNullException(nameof(rowKeySelector));
            _logger = logger;
            _blobClient = blobServiceClient;

            Name = SanitizeContainerName(typeof(TEntity).Name);
        }

        public string Name { get; }

        public async Task<Result> AddAsync(TEntity entity)
        {
            var partitionKey = SanitizeKey(_partitionKeySelector(entity));
            var rowKey = SanitizeKey(_rowKeySelector(entity));

            var serializedEntity = JsonCodec.Encode(entity);

            try
            {
                await CreateIfNotExistsAsync();

                var blobClient = _blobClient.GetBlobContainerClient(Name)
                    .GetBlobClient($"{partitionKey}/{rowKey}");

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(serializedEntity));
                await blobClient.UploadAsync(stream, true);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding entity");
                return Result.Failure($"Error: {ex.Message}");
            }
        }

        public async Task<Result> UpdateAsync(TEntity entity)
        {
            var partitionKey = SanitizeKey(_partitionKeySelector(entity));
            var rowKey = SanitizeKey(_rowKeySelector(entity));

            var serializedEntity = JsonCodec.Encode(entity);

            try
            {
                await CreateIfNotExistsAsync();

                var blobClient = _blobClient.GetBlobContainerClient(Name)
                    .GetBlobClient($"{partitionKey}/{rowKey}");

                if (await blobClient.ExistsAsync())
                {
                    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(serializedEntity));
                    await blobClient.UploadAsync(stream, true);
                }
                else
                {
                    return Result.Failure("Entity does not exist, unable to update.");
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating entity");
                return Result.Failure($"Error: {ex.Message}");
            }
        }

        public async Task<Result> UpsertAsync(TEntity entity)
        {
            var partitionKey = SanitizeKey(_partitionKeySelector(entity));
            var rowKey = SanitizeKey(_rowKeySelector(entity));

            var serializedEntity = JsonCodec.Encode(entity);

            try
            {
                await CreateIfNotExistsAsync();

                var blobClient = _blobClient.GetBlobContainerClient(Name)
                    .GetBlobClient($"{partitionKey}/{rowKey}");

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(serializedEntity));
                await blobClient.UploadAsync(stream, true);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting entity");
                return Result.Failure($"Error: {ex.Message}");
            }
        }

        public async Task<Result<TEntity>> GetAsync(string partitionKey, string rowKey)
        {
            partitionKey = SanitizeKey(partitionKey);
            rowKey = SanitizeKey(rowKey);

            try
            {
                await CreateIfNotExistsAsync();

                var blobClient = _blobClient.GetBlobContainerClient(Name)
                    .GetBlobClient($"{partitionKey}/{rowKey}");

                if (await blobClient.ExistsAsync())
                {
                    var blobDownloadInfo = await blobClient.DownloadAsync();

                    await using var stream = blobDownloadInfo.Value.Content;
                    using var reader = new StreamReader(stream);

                    var serializedEntity = await reader.ReadToEndAsync();
                    var entity = JsonCodec.Decode<TEntity>(serializedEntity)!;

                    return Result.Success(entity);
                }

                return Result.Failure<TEntity>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity");
                return Result.Failure<TEntity>($"Error: {ex.Message}");
            }
        }

        public async Task<IEnumerable<TEntity>> GetAsync(string partitionKey)
        {
            partitionKey = SanitizeKey(partitionKey);

            try
            {
                await CreateIfNotExistsAsync();

                var containerClient = _blobClient.GetBlobContainerClient(Name);
                var blobs = containerClient.GetBlobsByHierarchyAsync(prefix: partitionKey);

                var entities = new List<TEntity>();

                await foreach (var blobItem in blobs)
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Blob.Name);
                    var download = await blobClient.DownloadAsync();

                    await using var stream = download.Value.Content;
                    using var reader = new StreamReader(stream);

                    var serializedEntity = await reader.ReadToEndAsync();
                    var entity = JsonCodec.Decode<TEntity>(serializedEntity)!;

                    entities.Add(entity);
                }

                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entities by {PartitionKey}", partitionKey);
                return [];
            }
        }

        public async Task<IEnumerable<TEntity>> ListAsync()
        {
            try
            {
                await CreateIfNotExistsAsync();

                var containerClient = _blobClient.GetBlobContainerClient(Name);
                var blobs = containerClient.GetBlobsByHierarchyAsync();

                var entities = new List<TEntity>();

                await foreach (var blobItem in blobs)
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Blob.Name);
                    var download = await blobClient.DownloadAsync();

                    await using var stream = download.Value.Content;
                    using var reader = new StreamReader(stream);

                    var serializedEntity = await reader.ReadToEndAsync();
                    var entity = JsonCodec.Decode<TEntity>(serializedEntity)!;

                    entities.Add(entity);
                }

                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entities");
                return [];
            }
        }

        public async Task<Result> DeleteAsync(string partitionKey, string rowKey)
        {
            partitionKey = SanitizeKey(partitionKey);
            rowKey = SanitizeKey(rowKey);

            try
            {
                await CreateIfNotExistsAsync();

                var blobClient = _blobClient.GetBlobContainerClient(Name)
                    .GetBlobClient($"{partitionKey}/{rowKey}");

                if (await blobClient.ExistsAsync())
                {
                    await blobClient.DeleteIfExistsAsync();
                    Console.WriteLine($"Blob with PartitionKey = {partitionKey}, RowKey = {rowKey} deleted successfully.");
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting entity");
                return Result.Failure($"Error: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(string partitionKey)
        {
            partitionKey = SanitizeKey(partitionKey);

            try
            {
                await CreateIfNotExistsAsync();

                var containerClient = _blobClient.GetBlobContainerClient(Name);
                await foreach (var blobItem in containerClient.GetBlobsByHierarchyAsync(prefix: partitionKey))
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Blob.Name);
                    await blobClient.DeleteIfExistsAsync();
                    Console.WriteLine($"Deleted blob: {blobItem.Blob.Name}");
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting entities");
                return Result.Failure($"Error: {ex.Message}");
            }
        }

        public async Task<Result> CreateIfNotExistsAsync()
        {
            try
            {
                var containerClient = _blobClient.GetBlobContainerClient(Name);
                await containerClient.CreateIfNotExistsAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating blob table");
                return Result.Failure($"Error creating blob table: {ex.Message}");
            }
        }

        private static string SanitizeKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            }

            return key.Trim()
                .Replace(" ", "_")
                .Replace("\\", "_")
                .Replace("/", "_");
        }

        private static string SanitizeContainerName(string containerName)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentException("Container name cannot be null or empty.", nameof(containerName));
            }

            return containerName.Trim()
                .ToLower()
                .Replace(" ", "_")
                .Replace("<", "")
                .Replace(">", "")
                .Replace(",", "")
                .Replace(".", "-")
                .Replace("_", "-")
                .TrimStart('-')
                .TrimEnd('-');
        }
    }
}
