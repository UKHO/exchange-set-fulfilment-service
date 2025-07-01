using Azure.Storage;
using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.Builder.S100.Factories
{
    internal class BlobClientFactory
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
                    var blobUri = new Uri($"{blobConnectionString}/{blobContainerName}/{blobName}");

                    return new BlobClient(blobUri, new StorageSharedKeyCredential(AzuriteAccountName, AzuriteKey));
                default:
                    return new BlobClient(blobConnectionString, blobContainerName, blobName);
            }
        }
    }
}
