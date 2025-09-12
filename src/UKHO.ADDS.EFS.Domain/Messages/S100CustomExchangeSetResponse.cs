namespace UKHO.ADDS.EFS.Messages
{
    /// <summary>
    /// Response model for S100 exchange set endpoints
    /// </summary>
    internal class S100CustomExchangeSetResponse
    {
        /// <summary>
        /// Links to related resources
        /// </summary>
        public required ExchangeSetLinks Links { get; set; }

        /// <summary>
        /// The expiry date and time for the exchange set URL
        /// </summary>
        public required DateTime ExchangeSetUrlExpiryDateTime { get; set; }

        /// <summary>
        /// Number of products explicitly requested
        /// </summary>
        public int RequestedProductCount { get; set; }

        /// <summary>
        /// Number of products that have data included in the produced Exchange Set
        /// </summary>
        public int ExchangeSetProductCount { get; set; }

        /// <summary>
        /// Number of requested products that are already up to date
        /// </summary>
        public int RequestedProductsAlreadyUpToDateCount { get; set; }

        /// <summary>
        /// Products that were requested but not included in the exchange set
        /// </summary>
        public List<S100ProductNotInExchangeSet> RequestedProductsNotInExchangeSet { get; set; } = new();

        /// <summary>
        /// The FSS Batch ID associated with the exchange set
        /// </summary>
        public string? FssBatchId { get; set; }
    }
}
