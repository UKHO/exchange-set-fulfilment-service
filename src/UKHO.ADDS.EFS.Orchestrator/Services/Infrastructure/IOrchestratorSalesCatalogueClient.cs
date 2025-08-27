using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Products;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure
{
    internal interface IOrchestratorSalesCatalogueClient
    {
        /// <summary>
        ///     Retrieves S100 products that have been modified since a specific date.
        /// </summary>
        /// <param name="sinceDateTime">Optional date and time to filter products that have changed since this time.</param>
        /// <param name="job">The job</param>
        /// <returns>
        ///     A tuple containing:
        ///     - s100SalesCatalogueData: The response from the Sales Catalogue API.
        ///     - LastModified: The timestamp when the data was last modified. Will be the original sinceDateTime if response is
        ///     NotModified.
        /// </returns>
        /// <remarks>
        ///     The method returns an empty response with the original sinceDateTime when an error occurs or when
        ///     an unexpected HTTP status code is returned from the API.
        /// </remarks>
        Task<(ProductList s100SalesCatalogueData, DateTime? LastModified)> GetS100ProductVersionListAsync(DateTime? sinceDateTime, Job job);

        /// <summary>
        ///     Retrieves S100 product names and their details from the Sales Catalogue Service.
        /// </summary>
        /// <param name="productNames">A collection of product names to retrieve.</param>
        /// <param name="job">The job context for the request.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        ///     The response containing product details or an empty response if an error occurs.
        /// </returns>
        Task<ProductEditionList> GetS100ProductEditionListAsync(IEnumerable<ProductName> productNames, Job job, CancellationToken cancellationToken);
    }
}
