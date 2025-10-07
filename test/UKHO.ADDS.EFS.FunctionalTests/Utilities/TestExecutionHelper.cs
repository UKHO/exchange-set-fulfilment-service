using System.Text.Json;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.FunctionalTests.Assertions;
using UKHO.ADDS.EFS.FunctionalTests.Diagnostics;
using UKHO.ADDS.EFS.FunctionalTests.Http;
using UKHO.ADDS.EFS.FunctionalTests.IO;

namespace UKHO.ADDS.EFS.FunctionalTests.Utilities
{
    public class TestExecutionHelper
    {
        private static async Task AwaitJobCompletionAndAssertOnResults(string requestId, string zipFileName, string[]? productNames = null, bool assertCallbackTxtFile = false, string batchId = "", string responseContent = "")
        {
            TestOutput.WriteLine($"Started waiting for job completion ... {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            var responseJobStatus = await OrchestratorClient.WaitForJobCompletionAsync(requestId);
            await ExchangeSetApiAssertions.CheckJobCompletionStatus(responseJobStatus);
            TestOutput.WriteLine($"Finished waiting for job completion ... {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

            var responseBuildStatus = await OrchestratorClient.GetBuildStatusAsync(requestId);
            await ExchangeSetApiAssertions.CheckBuildStatus(responseBuildStatus);

            if (assertCallbackTxtFile)
            {
                TestOutput.WriteLine($"Trying to download file callback-response-{batchId}.txt ... {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                var callbackTxtFilePath = await MockFilesClient.DownloadCallbackTxtAsync(batchId);
                CallbackResponseAssertions.CompareCallbackResponse(responseContent, callbackTxtFilePath);
            }

            TestOutput.WriteLine($"Trying to download file V01X01_{requestId}.zip ... {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            var exchangeSetDownloadPath = await MockFilesClient.DownloadExchangeSetAsZipAsync(requestId);
            var sourceZipPath = Path.Combine(Infrastructure.AspireTestHost.ProjectDirectory!, "TestData", zipFileName);

            ZipArchiveAssertions.CompareZipFilesExactMatch(sourceZipPath, exchangeSetDownloadPath, productNames);
        }

        public static async Task ExecuteFullExchangeSetTestSteps(
            string requestId,
            object payload,
            string endpoint,
            string zipFileName = "",
            string expectedJobStatus = "submitted",
            string expectedBuildStatus = "scheduled",
            string[]? productNames = null)
        {
            var responseJobSubmit = await OrchestratorClient.PostRequestAsync(requestId, payload, endpoint);
            await ExchangeSetApiAssertions.FullExchangeSetJobsResponseChecks(requestId, responseJobSubmit, expectedJobStatus, expectedBuildStatus);

            if (zipFileName != "")
            {
                await AwaitJobCompletionAndAssertOnResults(requestId, zipFileName, productNames);
            }
        }

        public static async Task ExecuteCustomExchangeSetTestSteps(
            string requestId,
            object payload,
            string endpoint,
            string zipFileName,
            int expectedRequestedProductCount,
            int expectedExchangeSetProductCount,
            bool assertCallbackTxtFile = false,
            string[]? productNames = null)
        {
            var responseJobSubmit = await OrchestratorClient.PostRequestAsync(requestId, payload, endpoint);
            var responseContent = await ExchangeSetApiAssertions.CustomExchangeSetReqResponseChecks(
                requestId, responseJobSubmit, expectedRequestedProductCount, expectedExchangeSetProductCount);
            
            // Extract batchId
            var batchId = responseContent.Contains("fssBatchId") 
                ? JsonDocument.Parse(responseContent).RootElement.GetProperty("fssBatchId").GetString()! 
                : "";

            await AwaitJobCompletionAndAssertOnResults(requestId, zipFileName, productNames, assertCallbackTxtFile, batchId, responseContent);
        }
    }
}
