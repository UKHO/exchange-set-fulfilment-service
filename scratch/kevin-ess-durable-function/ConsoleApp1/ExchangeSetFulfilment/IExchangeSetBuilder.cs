
namespace ExchangeSetApiClient
{
    public interface IExchangeSetBuilder
    {
        Task AddContent(string exchangeSetID, string resourceLocation);
        Task<string> BuildAndDownloadExchangeSet(string exchangeSetID, string resourceLocation, string privateKey, string certificate, string destination);
        Task CreateExchangeSet(string exchangeSetID);
        Task DownloadExchangeSet(string exchangeSetID);
        Task SignExchangeSet(string exchangeSetID);
    }
}