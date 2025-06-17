namespace UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Facades
{
    public class ExchangeSetDownloadAPIFacade
    {
        private const string DownloadExchangeApiEndpointTemplate = "https://localhost:62824/_admin/files/FSS/{0}.zip";

        public async Task<string> DownloadExchangeSetAsZipAsync(string correlationID)
        {
            
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var exchangeSetName = $"S100_ExchangeSet_{datePart}";
            var downloadUrl = string.Format(DownloadExchangeApiEndpointTemplate, exchangeSetName);

            using var client = new HttpClient();
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, downloadUrl);

            var zipResponse = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

            zipResponse.EnsureSuccessStatusCode();

            await using var zipStream = await zipResponse.Content.ReadAsStreamAsync();

            var destinationFilePath = Path.Combine(Path.GetTempPath(), "temp", $"{exchangeSetName}_{correlationID}.zip");

            await using var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await zipStream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();
            return destinationFilePath;
        }
    }
}
