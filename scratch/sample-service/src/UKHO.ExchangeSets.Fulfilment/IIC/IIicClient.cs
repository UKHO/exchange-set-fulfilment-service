using System.Net.Mime;
using UKHO.ExchangeSets.Fulfilment.IIC.Parameters;
using UKHO.Infrastructure.Results;

namespace UKHO.ExchangeSets.Fulfilment.IIC
{
    public interface IIicClient
    {
        Task<Result> AddContentAsync(string workspaceId, string exchangeSetId, string resourceLocation);
        Task<Result> AddExchangeSetAsync(string workspaceId, string exchangeSetId);
        Task<Result> AddResourceAsync(string workspaceId, ContentType contentType, string resourceName, FileParameter resourcedata);
        Task<Result> AddResourceContentAsync(string workspaceId, string exchangeSetId, string resourceName, CatalogType contentType, string productName);
        Task<Result> AddWorkspaceAsync(string workspaceId);
        Task<Result> DelContentAsync(string workspaceId, string exchangeSetId, string fileName, string path);
        Task<Result> DelExchangeSetAsync(string workspaceId, string exchangeSetId);
        Task<Result<Lock>> FreeExchangeSetAsync(string workspaceId, string exchangeSetId, bool? force);
        Task<Result> ListExchangeSetAsync(string workspaceId);
        Task<Result> ListResourceAsync(string workspaceId);
        Task<Result> ListWorkspaceAsync(string authKey);
        Task<Result<Lock>> LockExchangeSetAsync(string workspaceId, string exchangeSetId);
        Task<Result<Lock>> LockInfoAsync(string workspaceId, string exchangeSetId);
        Task<Result> ModWorkspaceAsync(string workspaceId, string key, string value);
        Task<Result> PackageExchangeSetAsync(string workspaceId, string exchangeSetId, string destination);
        Task<Result> SignContentAsync(string workspaceId, string exchangeSetId, string fileName, SignatureType? contentType);
        Task<Result> SignExchangeSetAsync(string workspaceId, string exchangeSetId, string privateKey, string certificate);
    }
}
