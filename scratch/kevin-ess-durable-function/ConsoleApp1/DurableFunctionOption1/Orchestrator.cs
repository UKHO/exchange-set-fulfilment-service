using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace ConsoleApp1.DurableFunctions
{
    public static class ESSFulfilmentOrchestratorFunction
    {
        [FunctionName("ESSFulfilmentOrchestratorFunction")]
        public static async Task Run(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            // Configure your retry policy.
            // - firstRetryInterval: The initial delay before the first retry attempt.
            // - maxNumberOfAttempts: The total number of attempts (including the initial call).
            // You can also specify other properties like BackoffCoefficient, MaxRetryInterval, and so on.
            var retryOptions = new RetryOptions(
                firstRetryInterval: TimeSpan.FromSeconds(5),
                maxNumberOfAttempts: 3)
            {
                // For exponential backoff, uncomment and adjust as needed.
                // BackoffCoefficient = 2,

                // You can also handle specific exceptions if desired. By default, it retries
                // on all unhandled exceptions. Example:
                // Handle = ex => ex is SomeTransientException
            };

            // 1. Get Basic Catalog from S-100 Fulfillment Service
            string basicCatalogInfo = await context.CallActivityWithRetryAsync<string>(
                "GetBasicCatalogueActivity",
                retryOptions,
                null
            );

            // 2. Get the Files from File Share Service
            List<string> downloadedFiles = await context.CallActivityWithRetryAsync<List<string>>(
                "GetFilesAPIActivity",
                retryOptions,
                basicCatalogInfo
            );

            // 3. Upload Files to IIC API spool folder
            string spoolFolderName = await context.CallActivityWithRetryAsync<string>(
                "UploadFilesToIICSpoolActivity",
                retryOptions,
                downloadedFiles
            );

            // 4. Create Exchange Set
            (string workspaceId, string exchangeSetId) = await context.CallActivityWithRetryAsync<(string, string)>(
                "CreateExchangeSetActivity",
                retryOptions,
                spoolFolderName
            );

            // 5. Add spool folder contents to Exchange Set
            await context.CallActivityWithRetryAsync(
                "AddContentToExchangeSetActivity",
                retryOptions,
                new { WorkspaceId = workspaceId, ExchangeSetId = exchangeSetId, SpoolFolder = spoolFolderName }
            );

            // 6. Sign the Exchange Set
            await context.CallActivityWithRetryAsync(
                "SignExchangeSetActivity",
                retryOptions,
                new { WorkspaceId = workspaceId, ExchangeSetId = exchangeSetId }
            );

            // 7. Download the Exchange Set (ZIP file)
            string zipFilePath = await context.CallActivityWithRetryAsync<string>(
                "DownloadExchangeSetActivity",
                retryOptions,
                new { WorkspaceId = workspaceId, ExchangeSetId = exchangeSetId }
            );

            // 8. Rename/Store the Exchange Set ZIP file
            string renamedZipPath = await context.CallActivityWithRetryAsync<string>(
                "RenameExchangeSetZipActivity",
                retryOptions,
                zipFilePath
            );

            // 9. Upload the Exchange Set somewhere else
            await context.CallActivityWithRetryAsync(
                "UploadExchangeSetActivity",
                retryOptions,
                renamedZipPath
            );

            // 10. Commit the Exchange Set
            await context.CallActivityWithRetryAsync(
                "CommitExchangeSetActivity",
                retryOptions,
                new { WorkspaceId = workspaceId, ExchangeSetId = exchangeSetId }
            );

            // 11. (Optional) Upload "Get All" Exchange Set API response
            await context.CallActivityWithRetryAsync(
                "UploadGetAllApiResponseActivity",
                retryOptions,
                new { WorkspaceId = workspaceId }
            );

            // Orchestrator ends here
        }
    }
}
