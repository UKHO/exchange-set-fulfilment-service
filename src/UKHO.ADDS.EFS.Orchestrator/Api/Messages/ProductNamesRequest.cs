namespace UKHO.ADDS.EFS.Orchestrator.Api.Messages
{
    /// <summary>
    ///     Request model for product names endpoint
    /// </summary>
    internal class ProductNamesRequest
    {
        /// <summary>
        ///     List of product names to request
        /// </summary>
        public required List<string> ProductNames { get; set; }
    }
}
