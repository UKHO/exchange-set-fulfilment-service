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

                // Check timestamp is within 1 minute of now (UTC)
                if (jsonDoc.RootElement.TryGetProperty("timestamp", out var timestampProp))
                {
                    DateTimeOffset timestamp;
                    if (timestampProp.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(timestampProp.GetString(), out var parsed))
                    {
                        timestamp = parsed;
                    }
                    else
                    {
                        Assert.Fail("The 'timestamp' property is not a valid ISO 8601 string.");
                        return;
                    }

                    var diff = (DateTimeOffset.UtcNow - timestamp).Duration();
                    Assert.That(diff <= TimeSpan.FromMinutes(1), $"The 'timestamp' property is not within 1 minute of current UTC time. Actual difference: {diff.TotalSeconds} seconds.");
                }
                else
                {
                    Assert.Fail("The 'timestamp' property is missing in the blob JSON.");
                }
            });
        }

    }

}
