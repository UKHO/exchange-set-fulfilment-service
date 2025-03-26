using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Tables.Infrastructure
{
    public interface ITable<TEntity> where TEntity : class
    {
        Task<Result> CreateTableIfNotExistsAsync();

        Task<Result> AddAsync(TEntity entity);

        Task<Result<TEntity?>> GetAsync(string partitionKey, string rowKey);

        Task<IEnumerable<TEntity>> GetAsync(string partitionKey);

        Task<IEnumerable<TEntity>> ListAsync();
    }
}
