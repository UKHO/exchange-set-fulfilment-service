
namespace UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Facades
{
    public class ExchangeSetDownloadAPIFacade
    {
        private readonly string DownloadExchangeApiEndpoint = "https://localhost:62824/_admin/files/FSS/S100_ExchangeSet_20250613.zip";

        public async Task DownloadExchangeSetAsZipAsync(string destinationFilePath, string servicePrefix, string fileName)
        {                     
           
            HttpClient _client = new HttpClient();

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, DownloadExchangeApiEndpoint);            

            var zipResponse = await _client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);                     
            
            // Ensure the response is successful
            zipResponse.EnsureSuccessStatusCode();

            // Corrected code: Use CopyToAsync instead of WriteAsync
            await using var zipStream = await zipResponse.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await zipStream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();

        }
        
    }
}
