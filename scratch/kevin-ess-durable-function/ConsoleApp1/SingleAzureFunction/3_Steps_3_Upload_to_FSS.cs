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
    public class Function_3_Steps_3_Upload_to_FSS
    {
        private readonly IFileShareService _fileShareService;

        public Function_3_Steps_3_Upload_to_FSS(IExchangeSetBuilder exchangeSetBuilder, ISalesCatalogueService salesCatalogueService, IFileShareService fileShareService)
        {
            _fileShareService = fileShareService;
        }

        [FunctionName("ProcessExchangeSet")]
        public async Task Run(
            [QueueTrigger("exchange-set-fulfilment-upload-to-fss-queue", Connection = "AzureWebJobsStorage")] string queueMessage,
            ILogger log)
        {
            string destination = "someDestination"; // Replace with actual value or logic to get it
            string batchId = "someBatchId"; // Replace with actual value or logic to get it
            string correlationId = "someCorrelationId"; // Replace with actual value or logic to get it

            log.LogInformation("Uploading Files to FSS started.");

            
            var products = new List<Object>(); // Replace with actual products list or logic to get it
            var message = new Object(); // Replace with actual message or logic to get it
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var uploadFiles = await _fileShareService.UploadFileToFileShareService(batchId, destination, correlationId, "someFileName");

            var committedBatch = await _fileShareService.CommitBatchToFss(batchId, correlationId, destination, "zip");

            log.LogInformation("Files Uploaded to FSS function completed successfully.");

            // Log or handle the result, catalogue, and batch as needed
        }
    }
}
