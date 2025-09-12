using UKHO.ADDS.EFS.Domain.Products;

namespace UKHO.ADDS.EFS.Orchestrator.Api.Messages
{
    /// <summary>
    /// Request model for product versions endpoint
    /// </summary>
    internal class ProductVersionsRequest
    {
        /// <summary>
        /// List of product versions to request
        /// </summary>
        public required List<ProductVersion> ProductVersions { get; set; }
    }
}
