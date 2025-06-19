using UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Support;

namespace UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Facades
{
    public class ExchangeSetDownloadAPIFacade
    {
        
        private readonly string _downloadExchangeApiEndpoint;
        private readonly string _exchangeSetName;

        public ExchangeSetDownloadAPIFacade()
        {
            TestConfiguration testConfiguration = new TestConfiguration();
            _downloadExchangeApiEndpoint = testConfiguration.DownloadExchangeApiEndpoint + "/{ServicePrefix}/{FileName}.zip";
            _exchangeSetName = testConfiguration.ExchangeSetName;            
        }

        public async Task<string> DownloadExchangeSetAsZipAsync(string servicePrefix, string correlationID)
        {
            // Build exchange set name with date pattern replacement if needed
            var datePattern = "yyyyMMdd";
            var exchangeSetName = _exchangeSetName.Contains(datePattern)
                ? _exchangeSetName.Replace(datePattern, DateTime.UtcNow.ToString(datePattern))
                : $"{_exchangeSetName}{correlationID}";

            // Build the final download URL
            var finalDownloadUrl = _downloadExchangeApiEndpoint
                .Replace("{ServicePrefix}", servicePrefix)
                .Replace("{FileName}", exchangeSetName);           

            using var client = new HttpClient();
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, finalDownloadUrl);

            var zipResponse = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

            zipResponse.EnsureSuccessStatusCode();

            await using var zipStream = await zipResponse.Content.ReadAsStreamAsync();

            var projectDirectory = AppContext.BaseDirectory;
            var destinationFilePath = Path.Combine(projectDirectory, "out", $"{exchangeSetName}_{correlationID}.zip");

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
