namespace UKHO.ADDS.EFS.Messages
{
    /// <summary>
    /// Request model for S100 product names endpoint
    /// </summary>
    internal class S100ProductNamesRequest
    {
        /// <summary>
        /// List of S100 product names to request
        /// </summary>
        public required List<string> ProductNames { get; set; }
        public string? CallbackUri { get; init; }
    }
}
