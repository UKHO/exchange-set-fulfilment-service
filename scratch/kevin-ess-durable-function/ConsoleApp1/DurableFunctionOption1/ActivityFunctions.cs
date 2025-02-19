using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace ConsoleApp1.DurableFunctions
{
    public static class ActivityFunctions
    {
        [FunctionName("GetBasicCatalogueActivity")]
        public static async Task<string> GetBasicCatalogueActivity(
            [ActivityTrigger] object input,
            ILogger log)
        {
            log.LogInformation("[GetBasicCatalogueActivity] Starting...");
            // TODO: Call S-100 Fulfillment Service: Get Basic Catalogue
            // e.g. using HttpClient to GET from your S-100 endpoint
            await Task.Delay(1000); // simulate
            log.LogInformation("[GetBasicCatalogueActivity] Done.");
            return "MockBasicCatalogInfo";
        }

        [FunctionName("GetFilesAPIActivity")]
        public static async Task<List<string>> GetFilesAPIActivity(
            [ActivityTrigger] string basicCatalogInfo,
            ILogger log)
        {
            log.LogInformation("[GetFilesAPIActivity] Starting...");
            // TODO: Use the basicCatalogInfo to figure out which files to download
            // Download them from File Share Service
            await Task.Delay(1000);
            log.LogInformation("[GetFilesAPIActivity] Done.");
            return new List<string> { "File1.bin", "File2.bin" };
        }

        [FunctionName("UploadFilesToIICSpoolActivity")]
        public static async Task<string> UploadFilesToIICSpoolActivity(
            [ActivityTrigger] List<string> downloadedFiles,
            ILogger log)
        {
            log.LogInformation("[UploadFilesToIICSpoolActivity] Starting...");
            // TODO: Upload files to IIC API spool folder.
            // Return spool folder name or path
            await Task.Delay(1000);
            log.LogInformation("[UploadFilesToIICSpoolActivity] Done.");
            return "SpoolFolderNameOrPath";
        }

        [FunctionName("CreateExchangeSetActivity")]
        public static async Task<(string, string)> CreateExchangeSetActivity(
            [ActivityTrigger] string spoolFolderName,
            ILogger log)
        {
            log.LogInformation("[CreateExchangeSetActivity] Starting...");
            // TODO: Call IIC API to create empty Exchange Set (GET /addExchangeSet/...)
            // Return (workspaceId, exchangeSetId)
            await Task.Delay(1000);
            log.LogInformation("[CreateExchangeSetActivity] Done.");
            return ("Workspace123", "ExchangeSetABC");
        }

        [FunctionName("AddContentToExchangeSetActivity")]
        public static async Task AddContentToExchangeSetActivity(
            [ActivityTrigger] dynamic input,
            ILogger log)
        {
            log.LogInformation("[AddContentToExchangeSetActivity] Starting...");
            string workspaceId = input.WorkspaceId;
            string exchangeSetId = input.ExchangeSetId;
            string spoolFolder = input.SpoolFolder;
            // TODO: GET /addContent/{workspaceID}/{exchangeSetID}?resourceLocation={spool folder location}
            await Task.Delay(1000);
            log.LogInformation("[AddContentToExchangeSetActivity] Done.");
        }

        [FunctionName("SignExchangeSetActivity")]
        public static async Task SignExchangeSetActivity(
            [ActivityTrigger] dynamic input,
            ILogger log)
        {
            log.LogInformation("[SignExchangeSetActivity] Starting...");
            // GET /signExchangeSet/{workspaceID}/{exchangeSetID}
            await Task.Delay(1000);
            log.LogInformation("[SignExchangeSetActivity] Done.");
        }

        [FunctionName("DownloadExchangeSetActivity")]
        public static async Task<string> DownloadExchangeSetActivity(
            [ActivityTrigger] dynamic input,
            ILogger log)
        {
            log.LogInformation("[DownloadExchangeSetActivity] Starting...");
            string workspaceId = input.WorkspaceId;
            string exchangeSetId = input.ExchangeSetId;
            // GET /extractExchangeSet/{workspaceID}/{exchangeSetID}
            // Return path to the downloaded .zip file
            await Task.Delay(1000);
            log.LogInformation("[DownloadExchangeSetActivity] Done.");
            return "/tmp/originalExchangeSet.zip";
        }

        [FunctionName("RenameExchangeSetZipActivity")]
        public static async Task<string> RenameExchangeSetZipActivity(
            [ActivityTrigger] string zipFilePath,
            ILogger log)
        {
            log.LogInformation("[RenameExchangeSetZipActivity] Starting...");
            // Example: rename from "originalExchangeSet.zip" to "MyFinalExchangeSet.zip"
            // Return new file path
            await Task.Delay(1000);
            log.LogInformation("[RenameExchangeSetZipActivity] Done.");
            return "/tmp/MyFinalExchangeSet.zip";
        }

        [FunctionName("UploadExchangeSetActivity")]
        public static async Task UploadExchangeSetActivity(
            [ActivityTrigger] string renamedZipPath,
            ILogger log)
        {
            log.LogInformation("[UploadExchangeSetActivity] Starting...");
            // TODO: Upload the .zip to Azure Storage or elsewhere
            await Task.Delay(1000);
            log.LogInformation("[UploadExchangeSetActivity] Done.");
        }

        [FunctionName("CommitExchangeSetActivity")]
        public static async Task CommitExchangeSetActivity(
            [ActivityTrigger] dynamic input,
            ILogger log)
        {
            log.LogInformation("[CommitExchangeSetActivity] Starting...");
            string workspaceId = input.WorkspaceId;
            string exchangeSetId = input.ExchangeSetId;
            // e.g. GET /commitExchangeSet/{workspaceID}/{exchangeSetID}
            await Task.Delay(1000);
            log.LogInformation("[CommitExchangeSetActivity] Done.");
        }

        [FunctionName("UploadGetAllApiResponseActivity")]
        public static async Task UploadGetAllApiResponseActivity(
            [ActivityTrigger] dynamic input,
            ILogger log)
        {
            log.LogInformation("[UploadGetAllApiResponseActivity] Starting...");
            string workspaceId = input.WorkspaceId;
            // e.g. GET /GetAll or some other endpoint, then store the response
            await Task.Delay(1000);
            log.LogInformation("[UploadGetAllApiResponseActivity] Done.");
        }

        [FunctionName("NotifyExternalServiceActivity")]
        public static async Task NotifyExternalServiceActivity(
            [ActivityTrigger] dynamic input,
            ILogger log)
        {
            log.LogInformation("[NotifyExternalServiceActivity] Starting...");
            string exchangeSetId = input.ExchangeSetId;
            // TODO: e.g. send an email, or POST to external notification service
            await Task.Delay(1000);
            log.LogInformation("[NotifyExternalServiceActivity] Done.");
        }
    }
}
