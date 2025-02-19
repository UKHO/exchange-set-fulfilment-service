using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExchangeSetFulfilment
{
    public class S100ExchangeSetBuilder
    {
        public async Task BuildExchangeSet(List<Product> products)
        {
            // 2. Get the Files from File Share Service
            List<string> downloadedFiles = await GetFilesAPI(products);

            // 3. Upload Files to IIC spool folder
            string spoolFolderName = await UploadFilesToIICSpool(downloadedFiles);

            // 4. Create Exchange Set
            var (workspaceId, exchangeSetId) = await CreateExchangeSet(spoolFolderName);

            // 5. Add spool folder contents to Exchange Set
            await AddContentToExchangeSet(workspaceId, exchangeSetId, spoolFolderName);

            // 6. Sign the Exchange Set
            await SignExchangeSet(workspaceId, exchangeSetId);

            // 7. Download the Exchange Set (ZIP file)
            string zipFilePath = await DownloadExchangeSet(workspaceId, exchangeSetId);

            // 8. Rename/Store the Exchange Set ZIP file
            string renamedZipPath = await RenameExchangeSetZip(zipFilePath);

            // 9. Upload the Exchange Set (to e.g. Azure Blob Storage)
            await UploadExchangeSet(renamedZipPath);

            // 10. Commit the Exchange Set
            await CommitExchangeSet(workspaceId, exchangeSetId);

            // 12. Notify external service that a new Exchange Set is available
            await NotifyExternalService(exchangeSetId);
        }

        private async Task<List<string>> GetFilesAPI(List<Product> products)
        {
            await Task.Delay(500);
            return new List<string> { "File1.bin", "File2.bin" };
        }

        private async Task<string> UploadFilesToIICSpool(List<string> files)
        {
            await Task.Delay(500);
            return "SpoolFolderNameOrPath";
        }

        private async Task<(string workspaceId, string exchangeSetId)> CreateExchangeSet(string spoolFolderName)
        {
            await Task.Delay(500);
            return ("Workspace123", "ExchangeSetABC");
        }

        private async Task AddContentToExchangeSet(string workspaceId, string exchangeSetId, string spoolFolderName)
        {
            await Task.Delay(500);
        }

        private async Task SignExchangeSet(string workspaceId, string exchangeSetId)
        {
            await Task.Delay(500);
        }

        private async Task<string> DownloadExchangeSet(string workspaceId, string exchangeSetId)
        {
            await Task.Delay(500);
            return "/tmp/originalExchangeSet.zip";
        }

        private async Task<string> RenameExchangeSetZip(string zipFilePath)
        {
            await Task.Delay(500);
            return "/tmp/MyFinalExchangeSet.zip";
        }

        private async Task UploadExchangeSet(string finalZipPath)
        {
            await Task.Delay(500);
        }

        private async Task CommitExchangeSet(string workspaceId, string exchangeSetId)
        {
            await Task.Delay(500);
        }

        private async Task NotifyExternalService(string exchangeSetId)
        {
            await Task.Delay(500);
        }
    }
}
