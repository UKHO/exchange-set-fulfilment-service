namespace UKHO.ADDS.Mocks.EFS.Models
{
    /// <summary>
    /// Represents an S100 exchange set file with metadata
    /// </summary>
    public class S100ExchangeSetFile
    {
        /// <summary>
        /// Gets or sets the file name
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MIME type of the file
        /// </summary>
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file stream containing the file data
        /// </summary>
        public Stream FileStream { get; set; } = Stream.Null;

        /// <summary>
        /// Gets or sets the file size in bytes
        /// </summary>
        public long Size { get; set; }
    }
}
