using UKHO.ADDS.EFS.S100.API.FunctionalTests.Support;

namespace UKHO.ADDS.EFS.S100.API.FunctionalTests.Facades
{
    public class ExchangeSetDownloadAPIFacade
    {
        
        private readonly string _downloadExchangeApiEndpoint;
        private readonly string _exchangeSetName;

        public ExchangeSetDownloadAPIFacade()
        {
            TestConfiguration testConfiguration = new TestConfiguration();
            _downloadExchangeApiEndpoint = testConfiguration.DownloadExchangeApiEndpoint;
            _exchangeSetName = testConfiguration.ExchangeSetName;            
        }

        public async Task<string> DownloadExchangeSetAsZipAsync(string servicePrefix, string correlationID)
        {            
            var exchangeSetName = _exchangeSetName.Replace("{jobId}", correlationID);

            // Build the final download URL
            var finalDownloadUrl = $"{_downloadExchangeApiEndpoint}/{servicePrefix}/{exchangeSetName}.zip";               

            using var client = new HttpClient();
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, finalDownloadUrl);

            var zipResponse = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

            zipResponse.EnsureSuccessStatusCode();

            await using var zipStream = await zipResponse.Content.ReadAsStreamAsync();

            var projectDirectory = AppContext.BaseDirectory;
            var destinationFilePath = Path.Combine(TestConfiguration.ProjectDirectory, "out", $"{exchangeSetName}.zip");

            // Ensure the directory exists
            var destinationDirectory = Path.GetDirectoryName(destinationFilePath);
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory!);
            }            

            await using var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await zipStream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();
            return destinationFilePath;
        }
    }
}
