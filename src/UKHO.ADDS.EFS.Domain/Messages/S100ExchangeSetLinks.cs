namespace UKHO.ADDS.EFS.Messages
{
    /// <summary>
    /// Links to related resources for S100 exchange set
    /// </summary>
    internal class S100ExchangeSetLinks
    {
        /// <summary>
        /// Link to exchange set batch status
        /// </summary>
        public required S100Link ExchangeSetBatchStatusUri { get; set; }

        /// <summary>
        /// Link to exchange set batch details
        /// </summary>
        public required S100Link ExchangeSetBatchDetailsUri { get; set; }

        /// <summary>
        /// Link to exchange set file (optional)
        /// </summary>
        public S100Link? ExchangeSetFileUri { get; set; }
    }
}
