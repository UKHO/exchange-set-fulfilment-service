using Azure.Data.Tables;
using UKHO.ADDS.Infrastructure.Results;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure
{
    internal abstract class Table<TEntity> : ITable<TEntity> where TEntity : class
    {
        private readonly Func<TEntity, string> _partitionKeySelector;
        private readonly Func<TEntity, string> _rowKeySelector;
        private readonly TableClient _tableClient;

        protected Table(TableServiceClient tableServiceClient, Func<TEntity, string> partitionKeySelector, Func<TEntity, string> rowKeySelector)
        {
            _partitionKeySelector = partitionKeySelector ?? throw new ArgumentNullException(nameof(partitionKeySelector));
            _rowKeySelector = rowKeySelector ?? throw new ArgumentNullException(nameof(rowKeySelector));

            Name = SanitizeTableName(typeof(TEntity).Name);

            _tableClient = tableServiceClient.GetTableClient(Name);
        }

        public string Name { get; }

        public async Task<Result> CreateTableIfNotExistsAsync()
        {
            try
            {
                await _tableClient.CreateIfNotExistsAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
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

        public async Task<Result<TEntity?>> GetAsync(string partitionKey, string rowKey)
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
                return Enumerable.Empty<TEntity>();
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

        private static string SanitizeKey(string key) => key.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");

        private static string SanitizeTableName(string tableName)
        {
            return tableName
                .Replace("<", "_")
                .Replace(">", "_")
                .Replace(",", "_")
                .Replace(" ", "_");
        }
    }
}
