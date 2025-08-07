using System;
using Azure.Core;
using UKHO.Logging.EventHubLogProvider.AzureStorageEventLogging.Interfaces;

namespace UKHO.Logging.EventHubLogProvider.AzureStorageEventLogging.Models
{
    /// <summary>
    ///     The storage provider options model
    /// </summary>
    public class AzureStorageLogProviderOptions : IAzureStorageLogProviderOptions
    {
        /// <summary>
        ///     The Options model for the Azure Storage Log Provider
        /// </summary>
        /// <param name="blobContainerUri">The blob container url</param>
        /// <param name="credential">The managed identity for accessing the blob container</param>
        /// <param name="azureStorageLoggerEnabled">The azure storage enabled flag</param>
        /// <param name="successfulMessageTemplate">The successful message template</param>
        /// <param name="failedMessageTemplate">The failed message template</param>
        public AzureStorageLogProviderOptions(Uri blobContainerUri,
                                              TokenCredential credential,
                                              bool azureStorageLoggerEnabled,
                                              string successfulMessageTemplate,
                                              string failedMessageTemplate)
            : this(azureStorageLoggerEnabled, successfulMessageTemplate, failedMessageTemplate)
        {
            AzureStorageBlobContainerUri = blobContainerUri;
            if (!AzureStorageLoggerEnabled)
                return;
            
            AzureStorageCredential = credential ?? throw new NullReferenceException($"The {nameof(credential)} cannot be null when Azure storage option is set to enabled");
        }

        /// <summary>
        ///     The Options model for the Azure Storage Log Provider
        /// </summary>
        /// <param name="azureStorageContainerSasUrlString">The sas url</param>
        /// <param name="azureStorageLoggerEnabled">The azure storage enabled flag</param>
        /// <param name="successfulMessageTemplate">The successful message template</param>
        /// <param name="failedMessageTemplate">The failed message template</param>
        public AzureStorageLogProviderOptions(string azureStorageContainerSasUrlString,
                                              bool azureStorageLoggerEnabled,
                                              string successfulMessageTemplate,
                                              string failedMessageTemplate)
            : this(azureStorageLoggerEnabled, successfulMessageTemplate, failedMessageTemplate)
        {
            AzureStorageContainerSasUrlString = azureStorageContainerSasUrlString;

            if (!AzureStorageLoggerEnabled)
                return;
            
            if (string.IsNullOrEmpty(AzureStorageContainerSasUrlString))
                throw new NullReferenceException("The Azure storage container sas url cannot be null or empty when Azure storage option is set to enabled");
            AzureStorageContainerSasUrl = ValidateSasUrl(azureStorageContainerSasUrlString);
        }

        private AzureStorageLogProviderOptions(
            bool azureStorageLoggerEnabled,
            string successfulMessageTemplate,
            string failedMessageTemplate)
        {
            AzureStorageLoggerEnabled = azureStorageLoggerEnabled;
            SuccessfulMessageTemplate = string.IsNullOrEmpty(successfulMessageTemplate) | string.IsNullOrWhiteSpace(successfulMessageTemplate)
                ? throw new NullReferenceException("The successful message template cannot be null.empty or whitespace")
                : successfulMessageTemplate;
            FailedMessageTemplate = string.IsNullOrEmpty(failedMessageTemplate) | string.IsNullOrWhiteSpace(failedMessageTemplate)
                ? throw new NullReferenceException("The failed message template cannot be null.empty or whitespace")
                : failedMessageTemplate;
        }

        /// <summary>
        ///     The azure storage sas url string
        /// </summary>
        public string AzureStorageContainerSasUrlString { get; }
        /// <summary>
        ///     Enable the azure storage functionality
        /// </summary>
        public bool AzureStorageLoggerEnabled { get; }

        /// <summary>
        ///     The azure storage uri model
        /// </summary>
        public Uri AzureStorageContainerSasUrl { get; }
        /// <summary>
        ///     The azure storage blob container uri
        /// </summary>
        public Uri AzureStorageBlobContainerUri { get; }

        /// <summary>
        ///    Indicates whether Managed identity is used to authenticate with Storage account
        /// </summary>
        public bool IsUsingManagedIdentity
            => AzureStorageBlobContainerUri != null;

        /// <summary>
        ///      The <see cref="Azure.Core.TokenCredential"/> to use for authentication.
        /// </summary>
        public TokenCredential AzureStorageCredential { get; }

        /// <summary>
        ///     The azure storage result successful template
        /// </summary>
        public string SuccessfulMessageTemplate { get; private set; }

        /// <summary>
        ///     The azure storage result failed template
        /// </summary>
        public string FailedMessageTemplate { get; private set; }

        /// <summary>
        ///     Validates and converts a sas url string into a url model
        /// </summary>
        /// <param name="url">The url (string)</param>
        /// <returns>A url model</returns>
        private Uri ValidateSasUrl(string url)
        {
            var isValid = Uri.TryCreate(url, UriKind.Absolute, out Uri uri) && Uri.IsWellFormedUriString(url, UriKind.Absolute);

            if (isValid == false)
                throw new UriFormatException("Invalid sas url.");

            return uri;
        }
    }
}