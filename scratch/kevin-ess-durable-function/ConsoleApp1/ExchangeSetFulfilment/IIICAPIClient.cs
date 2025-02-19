//----------------------

namespace ExchangeSetApiClient
{
    public interface IIICAPIClient
    {
        string BaseUrl { get; set; }
        bool ReadResponseAsString { get; set; }

        Task<Message> AddContentAsync(string workspaceID, string exchangeSetID, string resourceLocation);
        Task<Message> AddExchangeSetAsync(string workspaceID, string exchangeSetID);
        Task<Message> AddresourceAsync(string workspaceID, ContentType contentType, string resourceName, FileParameter resourcedata);
        Task<Message> AddResourceContentAsync(string workspaceID, string exchangeSetID, string resourceName, ContentType2 contentType, string productName);
        Task<Message> AddworkspaceAsync(string workspaceID);
        Task<Message> DelContentAsync(string workspaceID, string exchangeSetID, string fileName, string path);
        Task<Message> DelExchangeSetAsync(string workspaceID, string exchangeSetID);
        Task<Lock> FreeExchangeSetAsync(string workspaceID, string exchangeSetID, bool? force);
        Task<Message> ListExchangeSetAsync(string workspaceID);
        Task<Message> ListresourceAsync(string workspaceID);
        Task<Message> ListWorkspaceAsync(string authkey);
        Task<Lock> LockExchangeSetAsync(string workspaceID, string exchangeSetID);
        Task<Lock> LockInfoAsync(string workspaceID, string exchangeSetID);
        Task<Message> ModWorkspaceAsync(string workspaceID, string key, string value);
        Task<Message> PackageexchangesetAsync(string workspaceID, string exchangeSetID, string destination);
        Task<Message> SignContentAsync(string workspaceID, string exchangeSetID, string fileName, ContentType3? contentType);
        Task<Message> SignExchangeSetAsync(string workspaceID, string exchangeSetID, string privateKey, string certificate);
        Task<Message> TestingAsync(string arg);
        Task<Message> TestingAsync(string arg, CancellationToken cancellationToken);
    }
}