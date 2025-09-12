namespace UKHO.ADDS.EFS.Messages
{
    /// <summary>
    /// Links to related resources for S100 exchange set
    /// </summary>
    internal class ExchangeSetLinks
    {
        /// <summary>
        /// Link to exchange set batch status
        /// </summary>
        public required Link ExchangeSetBatchStatusUri { get; set; }

        /// <summary>
        /// Link to exchange set batch details
        /// </summary>
        public required Link ExchangeSetBatchDetailsUri { get; set; }

        /// <summary>
        /// Link to exchange set file (optional)
        /// </summary>
        public Link? ExchangeSetFileUri { get; set; }
    }
}
