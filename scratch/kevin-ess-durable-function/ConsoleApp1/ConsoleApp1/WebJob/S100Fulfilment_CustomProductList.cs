using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ExchangeSetFulfilment;
using Newtonsoft.Json;
using Azure.Storage.Queues.Models;

namespace WebJobExample
{
    public class Functions
    {
        private readonly S100ExchangeSetBuilder _exchangeSetBuilder;

        public Functions(S100ExchangeSetBuilder exchangeSetBuilder)
        {
            _exchangeSetBuilder = exchangeSetBuilder;
        }

        // This method runs when a message is added to the queue:
        public async Task S100Fulfilment_CustomProductList([QueueTrigger("my-queue-name")] string queueMessage, ILogger log)
        {
            try
            {
                log.LogInformation("ProcessExchangeSet job started.");

                // 1. Get Basic Catalog from S-100 Fulfillment Service
                List<Product> products = await GetProducts(queueMessage);

                await _exchangeSetBuilder.BuildExchangeSet(products);

                log.LogInformation("ProcessExchangeSet job completed successfully.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while processing the exchange set.");
                // Possibly handle or rethrow
            }
        }

        private async Task<List<Product>> GetProducts(string queueMessage)
        {
            // 1. Call S-100 Fulfillment Service: GET basic catalogue
            await Task.Delay(500);
            return new List<Product>();
        }
    }
