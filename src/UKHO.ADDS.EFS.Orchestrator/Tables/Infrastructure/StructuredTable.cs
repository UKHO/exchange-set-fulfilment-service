using Azure.Data.Tables;
using Serilog;
using UKHO.ADDS.Infrastructure.Results;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure
{
    internal abstract class StructuredTable<TEntity> : ITable<TEntity> where TEntity : class
    {
        private readonly Func<TEntity, string> _partitionKeySelector;
        private readonly Func<TEntity, string> _rowKeySelector;
        private readonly TableClient _tableClient;

        protected StructuredTable(TableServiceClient tableServiceClient, Func<TEntity, string> partitionKeySelector, Func<TEntity, string> rowKeySelector)
        {
            _partitionKeySelector = partitionKeySelector ?? throw new ArgumentNullException(nameof(partitionKeySelector));
            _rowKeySelector = rowKeySelector ?? throw new ArgumentNullException(nameof(rowKeySelector));

            Name = SanitizeTableName(typeof(TEntity).Name);

            _tableClient = tableServiceClient.GetTableClient(Name);
        }

        public string Name { get; }

        public async Task<Result> CreateIfNotExistsAsync()
        {
            try
            {
                await _tableClient.CreateIfNotExistsAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                Log.Error($"Error creating table: {ex.Message}");
                return Result.Failure($"Error creating table: {ex.Message}");
            }
        }

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

        public async Task<Result<TEntity>> GetAsync(string partitionKey, string rowKey)
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

        public async Task<IEnumerable<TEntity>> GetAsync(string partitionKey)
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

        public async Task<IEnumerable<TEntity>> ListAsync()
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
                Log.Error($"Error updating entity: {ex.Message}");
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
                Log.Error($"Error upserting entity: {ex.Message}");
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
                Log.Information($"Entity with PartitionKey = {partitionKey}, RowKey = {rowKey} deleted successfully.");

                return Result.Success();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to delete entity with PartitionKey = {partitionKey}, RowKey = {rowKey}. Error: {ex.Message}");
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
                    Log.Information($"Entity with PartitionKey = {entity.PartitionKey}, RowKey = {entity.RowKey} deleted successfully.");
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to delete entity with PartitionKey = {entity.PartitionKey}, RowKey = {entity.RowKey}. Error: {ex.Message}");
                    return Result.Failure($"Error: {ex.Message}");
                }
            }

            return Result.Success();
        }

        private static string SanitizeKey(string key) => key.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");

        private static string SanitizeTableName(string tableName) =>
            tableName
                .Replace("<", "_")
                .Replace(">", "_")
                .Replace(",", "_")
                .Replace(" ", "_");
    }
}
