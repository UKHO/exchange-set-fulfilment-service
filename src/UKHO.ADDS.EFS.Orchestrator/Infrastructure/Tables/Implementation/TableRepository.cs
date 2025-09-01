using Azure.Data.Tables;
using UKHO.ADDS.EFS.Infrastructure.Tables;
using UKHO.ADDS.Infrastructure.Results;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Implementation
{
    internal abstract class TableRepository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly Func<TEntity, string> _partitionKeySelector;
        private readonly Func<TEntity, string> _rowKeySelector;
        private readonly TableClient _tableClient;

        protected TableRepository(string name, TableServiceClient tableServiceClient, Func<TEntity, string> partitionKeySelector, Func<TEntity, string> rowKeySelector)
        {
            _partitionKeySelector = partitionKeySelector ?? throw new ArgumentNullException(nameof(partitionKeySelector));
            _rowKeySelector = rowKeySelector ?? throw new ArgumentNullException(nameof(rowKeySelector));

            Name = name;

            _tableClient = tableServiceClient.GetTableClient(Name);
        }

        public string Name { get; }

        public async Task<Result> AddAsync(TEntity entity)
        {
            var partitionKey = SanitizeKey(_partitionKeySelector(entity));
            var rowKey = SanitizeKey(_rowKeySelector(entity));

            var serializedEntity = JsonCodec.Encode(entity);

            try
            {
                // Split the serialized JSON into chunks and store it in InternalEntity
                var internalEntity = JsonEntityBuilder.BuildEntity(serializedEntity, partitionKey, rowKey);

                // Add the entity to Azure Table Storage
                await _tableClient.AddEntityAsync(internalEntity);
                return Result.Success();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure($"Error: {ex.Message}");
            }
        }

        public Task<Result<TEntity>> GetUniqueAsync(string key)
        {
            return GetUniqueAsync(key, key);
        }

        public async Task<Result<TEntity>> GetUniqueAsync(string partitionKey, string rowKey)
        {
            partitionKey = SanitizeKey(partitionKey);
            rowKey = SanitizeKey(rowKey);

            var query = _tableClient.QueryAsync<JsonEntity>(e => e.PartitionKey == partitionKey && e.RowKey == rowKey);
            var entities = await query.ToListAsync();

            if (entities.Count == 0)
            {
                return Result.Failure<TEntity>()!;
            }

            var serializedEntity = JsonEntityBuilder.RebuildEntityData(entities.First());
            return JsonCodec.Decode<TEntity>(serializedEntity)!;
        }

        public async Task<IEnumerable<TEntity>> GetListAsync(string partitionKey)
        {
            partitionKey = SanitizeKey(partitionKey);

            var query = _tableClient.QueryAsync<JsonEntity>(e => e.PartitionKey == partitionKey);
            var entities = await query.ToListAsync();

            if (entities.Count == 0)
            {
                return [];
            }

            return entities.Select(entity =>
            {
                var serializedEntity = JsonEntityBuilder.RebuildEntityData(entity);
                return JsonCodec.Decode<TEntity>(serializedEntity)!;
            });
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            // TODO Continuation logic etc

            var query = _tableClient.QueryAsync<JsonEntity>(e => true);
            var results = await query.ToListAsync();

            return results.Select(entity =>
            {
                var serializedEntity = JsonEntityBuilder.RebuildEntityData(entity);
                return JsonCodec.Decode<TEntity>(serializedEntity)!;
            });
        }

        public async Task<Result> UpdateAsync(TEntity entity)
        {
            var partitionKey = SanitizeKey(_partitionKeySelector(entity));
            var rowKey = SanitizeKey(_rowKeySelector(entity));

            var serializedEntity = JsonCodec.Encode(entity);

            try
            {
                var existingEntity = await _tableClient.GetEntityAsync<JsonEntity>(partitionKey, rowKey);

                if (!existingEntity.HasValue)
                {
                    return Result.Failure("Entity does not exist, unable to update.");
                }

                var etag = existingEntity.Value.ETag;

                var internalEntity = JsonEntityBuilder.BuildEntity(serializedEntity, partitionKey, rowKey);

                await _tableClient.UpdateEntityAsync(internalEntity, etag, TableUpdateMode.Replace);

                return Result.Success();
            }
            catch (Exception ex)
            {
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
                var internalEntity = JsonEntityBuilder.BuildEntity(serializedEntity, partitionKey, rowKey);

                await _tableClient.UpsertEntityAsync(internalEntity);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(string partitionKey, string rowKey)
        {
            partitionKey = SanitizeKey(partitionKey);
            rowKey = SanitizeKey(rowKey);

            try
            {
                await _tableClient.DeleteEntityAsync(partitionKey, rowKey);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(string partitionKey)
        {
            partitionKey = SanitizeKey(partitionKey);

            var query = _tableClient.QueryAsync<JsonEntity>(e => e.PartitionKey == partitionKey);
            var entities = await query.ToListAsync();

            foreach (var entity in entities)
            {
                try
                {
                    // Delete each entity in the partition
                    await _tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
                }
                catch (Exception ex)
                {
                    return Result.Failure($"Error: {ex.Message}");
                }
            }

            return Result.Success();
        }

        public async Task<Result> CreateIfNotExistsAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _tableClient.CreateIfNotExistsAsync(stoppingToken);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error creating table: {ex.Message}");
            }
        }

        private static string SanitizeKey(string key) => key.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
    }
}
