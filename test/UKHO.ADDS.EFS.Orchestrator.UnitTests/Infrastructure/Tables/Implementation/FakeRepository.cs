using System.Collections.Concurrent;
using UKHO.ADDS.EFS.Domain.Services.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Infrastructure.Tables.Implementation
{
    /// <summary>
    ///     An in-memory fake implementation of IRepository for testing.
    ///     Supports independent partition and row key selectors.
    /// </summary>
    public class FakeRepository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly Func<TEntity, string> _partitionKeySelector;
        private readonly Func<TEntity, string> _rowKeySelector;

        private readonly ConcurrentDictionary<(string PartitionKey, string RowKey), TEntity> _store = new();

        /// <summary>
        ///     Creates a new FakeTable with specified key selectors.
        /// </summary>
        /// <param name="name">Logical name of the table.</param>
        /// <param name="partitionKeySelector">Function to extract PartitionKey from entity.</param>
        /// <param name="rowKeySelector">Function to extract RowKey from entity.</param>
        public FakeRepository(string name, Func<TEntity, string> partitionKeySelector, Func<TEntity, string> rowKeySelector)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));

            _partitionKeySelector = partitionKeySelector ?? throw new ArgumentNullException(nameof(partitionKeySelector));
            _rowKeySelector = rowKeySelector ?? throw new ArgumentNullException(nameof(rowKeySelector));
        }

        /// <summary>
        ///     Current contents of the table.
        /// </summary>
        public IReadOnlyCollection<TEntity> Items => _store.Values.ToList().AsReadOnly();

        public string Name { get; }

        public Task<Result> AddAsync(TEntity entity)
        {
            var key = GetKey(entity);

            if (!_store.TryAdd(key, entity))
            {
                return Task.FromResult(Result.Failure("Entity already exists."));
            }

            return Task.FromResult(Result.Success());
        }

        public Task<Result<TEntity>> GetUniqueAsync(string key)
        {
            // Enforce PartitionKey == key AND RowKey == key

            var match = _store
                .Where(kvp => kvp.Key.PartitionKey == key && kvp.Key.RowKey == key)
                .Select(kvp => kvp.Value)
                .FirstOrDefault();

            return match != null
                ? Task.FromResult(Result.Success(match))
                : Task.FromResult(Result.Failure<TEntity>("Not found."));
        }

        public Task<Result<TEntity>> GetUniqueAsync(string partitionKey, string rowKey)
        {
            var compositeKey = (partitionKey, rowKey);

            return _store.TryGetValue(compositeKey, out var entity)
                ? Task.FromResult(Result.Success(entity))
                : Task.FromResult(Result.Failure<TEntity>("Not found."));
        }

        public Task<IEnumerable<TEntity>> GetListAsync(string partitionKey)
        {
            var list = _store
                .Values
                .Where(e => _partitionKeySelector(e) == partitionKey)
                .ToList();

            return Task.FromResult<IEnumerable<TEntity>>(list);
        }

        public Task<IEnumerable<TEntity>> GetAllAsync() =>
            Task.FromResult<IEnumerable<TEntity>>(_store.Values.ToList());

        public Task<Result> UpdateAsync(TEntity entity)
        {
            var key = GetKey(entity);

            if (!_store.ContainsKey(key))
            {
                return Task.FromResult(Result.Failure("Entity does not exist."));
            }

            _store[key] = entity;

            return Task.FromResult(Result.Success());
        }

        public Task<Result> UpsertAsync(TEntity entity)
        {
            var key = GetKey(entity);
            _store[key] = entity;

            return Task.FromResult(Result.Success());
        }

        public Task<Result> DeleteAsync(string partitionKey, string rowKey)
        {
            var key = (partitionKey, rowKey);
            var removed = _store.TryRemove(key, out _);

            return removed ? Task.FromResult(Result.Success()) : Task.FromResult(Result.Failure("Entity not found."));
        }

        public Task<Result> DeleteAsync(string partitionKey)
        {
            var toRemove = _store.Keys
                .Where(k => k.PartitionKey == partitionKey)
                .ToList();

            foreach (var key in toRemove)
            {
                _store.TryRemove(key, out _);
            }

            return Task.FromResult(Result.Success());
        }

        public Task<Result> CreateIfNotExistsAsync(CancellationToken stoppingToken) => Task.FromResult(Result.Success());

        private (string PartitionKey, string RowKey) GetKey(TEntity entity) => (_partitionKeySelector(entity), _rowKeySelector(entity));

        /// <summary>
        ///     Seeds entities into the in-memory table for tests.
        /// </summary>
        public void Seed(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                var key = GetKey(entity);
                _store[key] = entity;
            }
        }
    }
}
