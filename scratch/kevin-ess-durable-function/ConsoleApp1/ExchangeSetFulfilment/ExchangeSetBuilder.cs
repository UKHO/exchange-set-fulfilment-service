using System;
using System.Reflection;
using System.Threading.Tasks;

namespace ExchangeSetApiClient
{
    public class ExchangeSetBuilder : IExchangeSetBuilder
    {
        private readonly IIICAPIClient _apiClient;
        private readonly string _workspaceID;

        public ExchangeSetBuilder(IIICAPIClient apiClient, string workspaceID)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _workspaceID = workspaceID ?? throw new ArgumentNullException(nameof(workspaceID));
        }

        public async Task CreateExchangeSet(string exchangeSetID)
        {
            try
            {
                await _apiClient.AddExchangeSetAsync(_workspaceID, exchangeSetID);
                Console.WriteLine("Exchange Set created.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating Exchange Set: " + ex.Message);
                throw;
            }
        }

        public async Task AddContent(string exchangeSetID, string resourceLocation)
        {
            try
            {
                await _apiClient.AddContentAsync(_workspaceID, exchangeSetID, resourceLocation); ;
                Console.WriteLine("Exchange Set created.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating Exchange Set: " + ex.Message);
                throw;
            }
        }

        public async Task SignExchangeSet(string exchangeSetID)
        {
            try
            {
                string privateKey = string.Empty;
                string certificate = string.Empty;
                await _apiClient.SignExchangeSetAsync(_workspaceID, exchangeSetID, privateKey, certificate);
                Console.WriteLine("Exchange Set created.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating Exchange Set: " + ex.Message);
                throw;
            }
        }

        public async Task DownloadExchangeSet(string exchangeSetID)
        {
            try
            {
                string destination = string.Empty;
                await _apiClient.PackageexchangesetAsync(_workspaceID, exchangeSetID, destination);
                Console.WriteLine("Exchange Set created.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating Exchange Set: " + ex.Message);
                throw;
            }
        }

        public async Task<string> BuildAndDownloadExchangeSet(string exchangeSetID, string resourceLocation, string privateKey, string certificate, string destination)
        {
            try
            {
                // Step 1: Create the Exchange Set
                await CreateExchangeSet(exchangeSetID);

                // Step 2: Add Content to the Exchange Set
                await AddContent(exchangeSetID, resourceLocation);
                Console.WriteLine("Content added to Exchange Set.");

                // Step 3: Sign the Exchange Set
                await SignExchangeSet(exchangeSetID);
                Console.WriteLine("Exchange Set signed.");

                // Step 4: Package and Download the Exchange Set
                await DownloadExchangeSet(exchangeSetID);
                Console.WriteLine("Exchange Set packaged and available for download at: " + destination);

                return destination;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error building Exchange Set: " + ex.Message);
                throw;
            }
        }
    }
}