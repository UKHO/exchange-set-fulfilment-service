using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.IIC
{
    public interface IToolClient
    {
        Task<Result> Ping();

        Task AddExchangeSetAsync(string workspaceRootPath, string exchangeSetId);
        Task AddContentAsync(string workspaceRootPath, string exchangeSetId);
        Task SignExchangeSetAsync(string workspaceRootPath, string exchangeSetId);
        Task ExtractExchangeSetAsync(string workspaceRootPath, string exchangeSetId);
    }
}
