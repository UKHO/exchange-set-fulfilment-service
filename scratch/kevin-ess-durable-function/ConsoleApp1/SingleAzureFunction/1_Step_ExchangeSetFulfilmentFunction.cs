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
    public class ExchangeSetFunction
    {
        private readonly IExchangeSetBuilder _exchangeSetBuilder;
        private readonly ISalesCatalogueService _salesCatalogueService;
        private readonly IFileShareService _fileShareService;

        public ExchangeSetFunction(IExchangeSetBuilder exchangeSetBuilder, ISalesCatalogueService salesCatalogueService, IFileShareService fileShareService)
        {
            _exchangeSetBuilder = exchangeSetBuilder;
            _salesCatalogueService = salesCatalogueService;
            _fileShareService = fileShareService;
        }

        [FunctionName("ProcessExchangeSet")]
        public async Task Run(
            [TimerTrigger("0 0 * * * *")] TimerInfo myTimer, // Runs every hour on the hour
            ILogger log)
        {
            string exchangeSetID = "someExchangeSetID"; // Replace with actual value or logic to get it
            string resourceLocation = "someResourceLocation"; // Replace with actual value or logic to get it
            string destination = "someDestination"; // Replace with actual value or logic to get it
            string batchId = "someBatchId"; // Replace with actual value or logic to get it
            string correlationId = "someCorrelationId"; // Replace with actual value or logic to get it
            string userOid = "someUserOid"; // Replace with actual value or logic to get it

            log.LogInformation("ProcessExchangeSet function started.");

            
            var products = new List<Object>(); // Replace with actual products list or logic to get it
            var message = new Object(); // Replace with actual message or logic to get it
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            string exchangeSetRootPath = "someExchangeSetRootPath"; // Replace with actual value or logic to get it
            string businessUnit = "someBusinessUnit"; // Replace with actual value or logic to get it

            var latestExchangeSet = await _fileShareService.GetBatchInfoBasedOnProducts(products, message, cancellationTokenSource, cancellationToken, exchangeSetRootPath, businessUnit);
            
            var catalogue = await _salesCatalogueService.GetBasicCatalogue(batchId, correlationId);

            var downloadFiles = _fileShareService.DownloadBatchFiles(latestExchangeSet, new List<string>(), "someDownloadPath", message, cancellationTokenSource, cancellationToken);

            var batch = await _fileShareService.CreateBatch(userOid, correlationId);

            await _exchangeSetBuilder.CreateExchangeSet(exchangeSetID);

            await _exchangeSetBuilder.AddContent(exchangeSetID, resourceLocation);

            await _exchangeSetBuilder.SignExchangeSet(exchangeSetID);

            await _exchangeSetBuilder.DownloadExchangeSet(exchangeSetID);

            var uploadFiles = _fileShareService.UploadFileToFileShareService(batchId, destination, correlationId, "someFileName");

            var committedBatch = _fileShareService.CommitBatchToFss(batchId, correlationId, destination, "zip");

            log.LogInformation("ProcessExchangeSet function completed successfully.");

            // Log or handle the result, catalogue, and batch as needed
        }
    }
}
