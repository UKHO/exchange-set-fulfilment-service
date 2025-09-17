
namespace UKHO.ADDS.EFS.Orchestrator.Api.Messages
{
    /// <summary>
    /// Request model for product versions endpoint
    /// </summary>
    internal class ProductVersionRequest
    {
        /// <summary>
        /// The unique product Name
        /// </summary>
        public string? ProductName { get; set; }

        /// <summary>
        /// The edition number
        /// </summary>
        public int? EditionNumber { get; set; }

        /// <summary>
        /// The update number, if applicable
        /// </summary>
        public int? UpdateNumber { get; set; }
    }
}
