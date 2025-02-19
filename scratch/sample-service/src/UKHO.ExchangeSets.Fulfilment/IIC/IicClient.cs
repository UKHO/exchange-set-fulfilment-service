using System.Globalization;
using System.Net.Mime;
using UKHO.Clients.Common.Configuration;
using UKHO.ExchangeSets.Fulfilment.IIC.Parameters;
using UKHO.Infrastructure.Results;

namespace UKHO.ExchangeSets.Fulfilment.IIC
{
    internal class IicClient : IIicClient
    {
        public IicClient(ClientConfiguration configuration)
        {
        }

        public Task<Result> AddContentAsync(string workspaceId, string exchangeSetId, string resourceLocation)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result> AddExchangeSetAsync(string workspaceId, string exchangeSetId) 
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result> AddResourceAsync(string workspaceId, ContentType contentType, string resourceName, FileParameter resourcedata)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result> AddResourceContentAsync(string workspaceId, string exchangeSetId, string resourceName, CatalogType contentType, string productName)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result> AddWorkspaceAsync(string workspaceId)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result> DelContentAsync(string workspaceId, string exchangeSetId, string fileName, string path)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result> DelExchangeSetAsync(string workspaceId, string exchangeSetId)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result<Lock>> FreeExchangeSetAsync(string workspaceId, string exchangeSetId, bool? force)
        {
            return Task.FromResult(Result.Success(new Lock() { Datetime = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture), LockID = exchangeSetId}));
        }

        public Task<Result> ListExchangeSetAsync(string workspaceId)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result> ListResourceAsync(string workspaceId)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result> ListWorkspaceAsync(string authKey)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result<Lock>> LockExchangeSetAsync(string workspaceId, string exchangeSetId)
        {
            return Task.FromResult(Result.Success(new Lock() { Datetime = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture), LockID = exchangeSetId }));
        }

        public Task<Result<Lock>> LockInfoAsync(string workspaceId, string exchangeSetId)
        {
            return Task.FromResult(Result.Success(new Lock() { Datetime = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture), LockID = exchangeSetId }));
        }

        public Task<Result> ModWorkspaceAsync(string workspaceId, string key, string value)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result> PackageExchangeSetAsync(string workspaceId, string exchangeSetId, string destination)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result> SignContentAsync(string workspaceId, string exchangeSetId, string fileName, SignatureType? contentType)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result> SignExchangeSetAsync(string workspaceId, string exchangeSetId, string privateKey, string certificate)
        {
            return Task.FromResult(Result.Success());
        }
    }
}
