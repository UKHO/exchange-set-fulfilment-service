using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.Builder.Common.Factories
{
    public class BlobClientFactory
    {
        public const string AzuriteAccountName = "devstoreaccount1";
        private const string AzuriteKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

        public BlobClient CreateBlobClient(IConfiguration configuration, string blobName)
        {
            var environment = configuration[BuilderEnvironmentVariables.AddsEnvironment]!;
            var blobConnectionString = configuration[BuilderEnvironmentVariables.BlobConnectionString]!;
            var blobContainerName = configuration[BuilderEnvironmentVariables.BlobContainerName]!;

            switch (environment)
            {
                case "local":
                    // We need to construct the client using the "URL" method for running locally so that the container network can connect to Azurite using host.docker.internal
                    var blobUri = new Uri($"{blobConnectionString}/{blobContainerName}/{blobName}");

                    return new BlobClient(blobUri, new StorageSharedKeyCredential(AzuriteAccountName, AzuriteKey));
                default:
                    return new BlobClient(blobConnectionString, blobContainerName, blobName);
            }
        }
    }
}
