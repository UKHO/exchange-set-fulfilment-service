using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.Infrastructure.Builders.Factories
{
    public class BlobClientFactory
    {
        public const string AzuriteAccountName = "devstoreaccount1";
        private const string AzuriteKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

        public BlobClient CreateBlobClient(IConfiguration configuration, string blobName)
        {
            var environment = configuration[BuilderEnvironmentVariables.AddsEnvironment]!;
            var blobEndpoint = configuration[BuilderEnvironmentVariables.BlobEndpoint]!;
            var blobContainerName = configuration[BuilderEnvironmentVariables.BlobContainerName]!;
            var blobUri = blobEndpoint.EndsWith('/') ? new Uri($"{blobEndpoint}{blobContainerName}/{blobName}") : new Uri($"{blobEndpoint}/{blobContainerName}/{blobName}");

            switch (environment)
            {
                case "local":
                    return new BlobClient(blobUri, new StorageSharedKeyCredential(AzuriteAccountName, AzuriteKey));
                default:
                    var clientId = configuration[BuilderEnvironmentVariables.AzureClientId]!;
                    var credential = new ManagedIdentityCredential(clientId);
                    return new BlobClient(blobUri, credential);
            }
        }
    }
}
