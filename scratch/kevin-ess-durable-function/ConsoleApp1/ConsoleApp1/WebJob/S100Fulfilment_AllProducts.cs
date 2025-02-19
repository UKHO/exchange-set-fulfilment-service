using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ExchangeSetFulfilment
{
    public class Functions
    {
        private readonly S100ExchangeSetBuilder _exchangeSetBuilder;

        public Functions(S100ExchangeSetBuilder exchangeSetBuilder)
        {
            _exchangeSetBuilder = exchangeSetBuilder;
        }

        // This method runs every hour, on the hour:
        // Adjust the CRON expression for your schedule
        [NoAutomaticTrigger] // Remove this if you'd prefer a manual test run, otherwise TimerTrigger
                             // [TimerTrigger("0 0 * * * *")] // Uncomment to schedule at top of every hour
        public async Task S100Fulfilment_AllProducts(/* [TimerTrigger("0 0 * * * *")] TimerInfo timer, */ ILogger log)
        {
            try
            {
                log.LogInformation("ProcessExchangeSet job started.");

                // 1. Get Basic Catalog from S-100 Fulfillment Service
                List<Product> products = await GetBasicCatalogueAsync();

                await _exchangeSetBuilder.BuildExchangeSet(products);

                log.LogInformation("ProcessExchangeSet job completed successfully.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while processing the exchange set.");
                // Possibly handle or rethrow
            }
        }

        private async Task<List<Product>> GetBasicCatalogueAsync()
        {
            // 1. Call S-100 Fulfillment Service: GET basic catalogue
            await Task.Delay(500);
            string jsonResponse = "[{\"productName\": \"101GB020111\", \"editionNumber\": 3, \"updateNumber\": 0}, {\"productName\": \"101GB010130\", \"editionNumber\": 1, \"updateNumber\": 0}]";
            return JsonConvert.DeserializeObject<List<Product>>(jsonResponse);
        }
    }

}
