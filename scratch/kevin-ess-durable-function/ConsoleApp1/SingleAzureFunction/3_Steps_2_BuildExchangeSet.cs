using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UKHO.FileShareService;
using System.Collections.Generic;
using System;
using System.Threading;

namespace ExchangeSetApiClient
{
    public class Function_3_Steps_2_BuildExchangeSet
    {
        private readonly IExchangeSetBuilder _exchangeSetBuilder;
        
        public Function_3_Steps_2_BuildExchangeSet(IExchangeSetBuilder exchangeSetBuilder)
        {
            _exchangeSetBuilder = exchangeSetBuilder;
        }

        [FunctionName("Function_3_Steps_2_BuildExchangeSet")]
        public async Task Run(
            [QueueTrigger("exchange-set-fulfilment-builder-queue", Connection = "AzureWebJobsStorage")] string queueMessage,
            ILogger log)
        {
            string exchangeSetID = "someExchangeSetID"; // Replace with actual value or logic to get it
            string resourceLocation = "someResourceLocation"; // Replace with actual value or logic to get it
            string privateKey = "somePrivateKey"; // Replace with actual value or logic to get it
            string certificate = "someCertificate"; // Replace with actual value or logic to get it
            string destination = "someDestination"; // Replace with actual value or logic to get it

            log.LogInformation("Building Exchange Set function started.");


            var products = new List<Object>(); // Replace with actual products list or logic to get it
            var message = new Object(); // Replace with actual message or logic to get it
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            await _exchangeSetBuilder.BuildAndDownloadExchangeSet(exchangeSetID, resourceLocation, privateKey, certificate, destination);

            log.LogInformation("Builidng Exchange Set function completed successfully.");

            // Log or handle the result, catalogue, and batch as needed
        }
    }
}
