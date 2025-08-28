namespace UKHO.ADDS.EFS.Messages
{
    /// <summary>
    /// Represents a product that was requested but not included in the exchange set
    /// </summary>
    internal class ProductNotInExchangeSet
    {
        /// <summary>
        /// The product name
        /// </summary>
        public required string ProductName { get; set; }

        /// <summary>
        /// The reason why the product was not included
        /// </summary>
        public required ProductNotIncludedReason Reason { get; set; }
    }
}
