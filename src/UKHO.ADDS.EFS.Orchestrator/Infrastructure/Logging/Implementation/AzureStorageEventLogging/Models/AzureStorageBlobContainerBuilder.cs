using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs;
using UKHO.Logging.EventHubLogProvider.AzureStorageEventLogging.Interfaces;

namespace UKHO.Logging.EventHubLogProvider.AzureStorageEventLogging.Models
{
    /// <summary>
    ///     The Azure storage blob container builder model
    /// </summary>
    public class AzureStorageBlobContainerBuilder : IAzureStorageBlobContainerBuilder
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="azureStorageLogProviderOptions">The log provider options</param>
        public AzureStorageBlobContainerBuilder(AzureStorageLogProviderOptions azureStorageLogProviderOptions)
        {
            AzureStorageLogProviderOptions = azureStorageLogProviderOptions;
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="azureStorageLogProviderOptions">The log provider options</param>
        /// <param name="blobContainerClient">The blob client</param>
        [ExcludeFromCodeCoverage] // Used by Tests Only...
        public AzureStorageBlobContainerBuilder(AzureStorageLogProviderOptions azureStorageLogProviderOptions, BlobContainerClient blobContainerClient)
        {
            AzureStorageLogProviderOptions = azureStorageLogProviderOptions;
            BlobContainerClient = blobContainerClient;
        }

        public AzureStorageLogProviderOptions AzureStorageLogProviderOptions { get; }
        public BlobContainerClient BlobContainerClient { get; private set; }

        /// <summary>
        ///     Builds the blob client
        /// </summary>
        public void Build()
        {
            if (AzureStorageLogProviderOptions != null && AzureStorageLogProviderOptions.AzureStorageLoggerEnabled)
            {
                BlobContainerClient = AzureStorageLogProviderOptions.IsUsingManagedIdentity
                    ? new BlobContainerClient(AzureStorageLogProviderOptions.AzureStorageBlobContainerUri, AzureStorageLogProviderOptions.AzureStorageCredential)
                    : new BlobContainerClient(AzureStorageLogProviderOptions.AzureStorageContainerSasUrl);
            }
        }
    }
}