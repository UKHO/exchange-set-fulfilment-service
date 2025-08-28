namespace UKHO.ADDS.EFS.Messages
{
    /// <summary>
    /// Reasons why a product might not be included in the exchange set
    /// </summary>
    internal enum ProductNotIncludedReason
    {
        /// <summary>
        /// The product has been withdrawn from the service
        /// </summary>
        ProductWithdrawn,

        /// <summary>
        /// The product is not part of the service (invalid or unknown product)
        /// </summary>
        InvalidProduct,

        /// <summary>
        /// The product has been cancelled and is beyond the retention period
        /// </summary>
        NoDataAvailableForCancelledProduct
    }
}
