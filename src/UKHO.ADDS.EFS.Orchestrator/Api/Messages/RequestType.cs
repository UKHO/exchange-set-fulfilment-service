namespace UKHO.ADDS.EFS.Orchestrator.Api.Messages
{
    /// <summary>
    /// Enumeration of S100 request types for pipeline processing
    /// </summary>
    internal enum RequestType
    {
        /// <summary>
        /// Request for specific product names
        /// </summary>
        ProductNames,

        /// <summary>
        /// Request for specific product versions
        /// </summary>
        ProductVersions,

        /// <summary>
        /// Request for updates since a specific date/time
        /// </summary>
        UpdatesSince,

        /// <summary>
        /// Request for any other internal processing not initiated by an external request
        /// </summary>
        Internal
    }
}
