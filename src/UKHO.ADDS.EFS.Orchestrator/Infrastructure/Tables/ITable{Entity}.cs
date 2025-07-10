using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables
{
    public interface ITable<TEntity> where TEntity : class
    {
        string Name { get; }

        Task<Result> AddAsync(TEntity entity);

        Task<Result<TEntity>> GetUniqueAsync(string key);

        Task<Result<TEntity>> GetUniqueAsync(string partitionKey, string rowKey);

        Task<IEnumerable<TEntity>> GetListAsync(string partitionKey);

        Task<IEnumerable<TEntity>> GetAllAsync();

        Task<Result> UpdateAsync(TEntity entity);

        Task<Result> UpsertAsync(TEntity entity);

        Task<Result> DeleteAsync(string partitionKey, string rowKey);

        Task<Result> DeleteAsync(string partitionKey);

        Task<Result> CreateIfNotExistsAsync(CancellationToken stoppingToken);
    }
}
