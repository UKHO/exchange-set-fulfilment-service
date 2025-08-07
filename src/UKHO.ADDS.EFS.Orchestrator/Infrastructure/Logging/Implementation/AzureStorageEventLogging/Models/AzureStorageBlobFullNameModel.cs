using System;

namespace UKHO.Logging.EventHubLogProvider.AzureStorageEventLogging.Models
{
    /// <summary>
    ///     The Blob Full Name Model
    /// </summary>
    public class AzureStorageBlobFullNameModel
    {
        /// <summary>
        ///     The constructor
        /// </summary>
        /// <param name="serviceName">The service name</param>
        /// <param name="path">The blob folder path</param>
        /// <param name="blobName">The blob name</param>
        public AzureStorageBlobFullNameModel(string serviceName, string path, string blobName)
        {
            ServiceName = string.IsNullOrEmpty(serviceName) ? throw new ArgumentNullException(nameof(serviceName)) : serviceName;
            Path = string.IsNullOrEmpty(path) ? throw new ArgumentNullException(nameof(path)) : path;
            BlobName = string.IsNullOrEmpty(blobName) ? throw new ArgumentNullException(nameof(blobName)) : blobName;
        }

        public string ServiceName { get; }
        public string Path { get; }
        public string BlobName { get; }
    }
}