using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure
{
    public interface ITable<TEntity> where TEntity : class
    {
        string Name { get; }

        Task<Result> CreateIfNotExistsAsync();

        Task<Result> AddAsync(TEntity entity);

        Task<Result<TEntity>> GetAsync(string partitionKey, string rowKey);

        Task<IEnumerable<TEntity>> GetAsync(string partitionKey);

        Task<IEnumerable<TEntity>> ListAsync();

        Task<Result> UpdateAsync(TEntity entity);

        Task<Result> UpsertAsync(TEntity entity);

        Task<Result> DeleteAsync(string partitionKey, string rowKey);

        Task<Result> DeleteAsync(string partitionKey);
    }
}
