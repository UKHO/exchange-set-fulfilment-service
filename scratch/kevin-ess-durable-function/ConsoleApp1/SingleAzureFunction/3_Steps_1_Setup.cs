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
    public class Function_3_Steps_1_Setup
    {
        private readonly ISalesCatalogueService _salesCatalogueService;
        private readonly IFileShareService _fileShareService;

        public Function_3_Steps_1_Setup(ISalesCatalogueService salesCatalogueService, IFileShareService fileShareService)
        {
            _salesCatalogueService = salesCatalogueService;
            _fileShareService = fileShareService;
        }

        [FunctionName("Function_3_Steps_1_Setup")]
        public async Task Run(
            [TimerTrigger("0 0 * * * *")] TimerInfo myTimer, // Runs every hour on the hour
            ILogger log)
        {
            string batchId = "someBatchId"; // Replace with actual value or logic to get it
            string correlationId = "someCorrelationId"; // Replace with actual value or logic to get it

            log.LogInformation("Setup for Building Exchange Set function started.");

            
            var products = new List<Object>(); // Replace with actual products list or logic to get it
            var message = new Object(); // Replace with actual message or logic to get it
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            string exchangeSetRootPath = "someExchangeSetRootPath"; // Replace with actual value or logic to get it
            string businessUnit = "someBusinessUnit"; // Replace with actual value or logic to get it

            var latestExchangeSet = await _fileShareService.GetBatchInfoBasedOnProducts(products, message, cancellationTokenSource, cancellationToken, exchangeSetRootPath, businessUnit);
            
            var catalogue = await _salesCatalogueService.GetBasicCatalogue(batchId, correlationId);

            var downloadFiles = _fileShareService.DownloadBatchFiles(latestExchangeSet, new List<string>(), "someDownloadPath", message, cancellationTokenSource, cancellationToken);


            log.LogInformation("Setup for Building Exchange Set completed successfully.");

            // Log or handle the result, catalogue, and batch as needed
        }
    }
}
