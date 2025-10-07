namespace UKHO.ADDS.EFS.Domain.Jobs
{
    /// <summary>
    /// Enumeration of exchange set types for pipeline processing
    /// </summary>
    public enum ExchangeSetType
    {
        /// <summary>
        /// Request for creating custom exchange set using ProductName endpoint
        /// </summary>
        ProductNames,

        /// <summary>
        /// Request for creating custom exchange set using ProductVersion endpoint
        /// </summary>
        ProductVersions,

        /// <summary>
        /// Request for creating custom exchange set using UpdatesSince endpoint
        /// </summary>
        UpdatesSince,

        /// <summary>
        /// Request for creating complete exchange set using scheduled job
        /// </summary>
        Complete
    }
}
