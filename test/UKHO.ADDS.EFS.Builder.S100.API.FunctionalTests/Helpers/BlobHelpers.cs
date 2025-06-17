using System.Text.Json;
using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Support;

namespace UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Helpers  {
    public class BlobHelpers
    {
        private readonly string _connectionString;
        private readonly string _containerName;

        public BlobHelpers()
        {
            TestConfiguration testConfiguration = new TestConfiguration();
            _connectionString = testConfiguration.AzureStorageConnectionString;            
            _containerName = testConfiguration.ExchangeSetContainerName;
        }

        public BlobClient GetBlobClient(string blobName)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            return containerClient.GetBlobClient(blobName);
        }

        public async Task<JsonDocument> DownloadBlobAsJsonAsync(string blobName)
        {
            var blobClient = GetBlobClient(blobName);
            var downloadResult = await blobClient.DownloadContentAsync();
            var json = downloadResult.Value.Content.ToString();
            return JsonDocument.Parse(json);
        }

        // Optionally, if you want to download to a file:
        public async Task DownloadBlobToFileAsync(string blobName, string destinationFilePath)
        {
            var blobClient = GetBlobClient(blobName);
            await blobClient.DownloadToAsync(destinationFilePath);
        }
        public async Task AssertBlobStateAndCorrelationIdAsync(string expectedState, string expectedCorrelationId)
        {
            var blobName = $"{_containerName}/{expectedCorrelationId}/{expectedCorrelationId}";
            var jsonDoc = await DownloadBlobAsJsonAsync(blobName);

            Assert.Multiple(() =>
            {
                Assert.That(jsonDoc.RootElement.GetProperty("state").GetString(), Is.EqualTo(expectedState), $"The 'state' property is not '{expectedState}'.");
                Assert.That(jsonDoc.RootElement.GetProperty("correlationId").GetString(), Is.EqualTo(expectedCorrelationId), "The 'correlationId' property does not match the expected value.");
            });
        }
    }  

}
