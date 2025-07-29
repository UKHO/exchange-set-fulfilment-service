using System;

namespace UKHO.Logging.EventHubLogProvider.AzureStorageEventLogging.Models
{
    /// <summary>
    ///     The azure storage event log result
    /// </summary>
    public class AzureStorageEventLogResult
    {
        /// <summary>
        ///     The default constructor
        /// </summary>
        /// <param name="reasonPhrase">The result reason phrase</param>
        /// <param name="statusCode">The http status code</param>
        /// <param name="requestId">The client request Id</param>
        /// <param name="fileSHA">The blob SHA 256</param>
        /// <param name="isStored">The flag that determines if the result was successful/failed</param>
        /// <param name="blobFullName">The blob full name</param>
        /// <param name="fileSize">The file size (optional)</param>
        /// <param name="modifiedDate">The modified date(optional)</param>
        public AzureStorageEventLogResult(string reasonPhrase,
                                          int statusCode,
                                          string requestId,
                                          string fileSHA,
                                          bool isStored,
                                          string blobFullName,
                                          long? fileSize = null,
                                          DateTime? modifiedDate = null)
        {
            BlobFullName = blobFullName;
            IsStored = isStored;
            ReasonPhrase = reasonPhrase;
            RequestId = requestId;
            FileSHA = fileSHA;
            StatusCode = statusCode;
            ModifiedDate = modifiedDate.HasValue ? modifiedDate.Value : DateTime.MinValue;
            FileSize = fileSize.HasValue ? fileSize.Value : 0;
        }

        public string ReasonPhrase { get; set; }
        public int StatusCode { get; set; }
        public string RequestId { get; set; }
        public string FileSHA { get; set; }
        public bool IsStored { get; set; }
        public string BlobFullName { get; set; }
        public long FileSize { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}