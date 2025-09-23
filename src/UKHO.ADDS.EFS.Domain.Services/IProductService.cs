using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;

namespace UKHO.ADDS.EFS.Domain.Services
{
    public interface IProductService
    {
        /// <summary>
        ///     Retrieves products that have been modified since a specific date.
        /// </summary>
        /// <param name="sinceDateTime">Optional date and time to filter products that have changed since this time.</param>
        /// <param name="job">The job</param>
        /// <returns>
        ///     A tuple containing:
        ///     - ProductList: The product list
        ///     - LastModified: The timestamp when the data was last modified. Will be the original sinceDateTime if response is
        ///     NotModified.
        /// </returns>
        /// <remarks>
        ///     The method returns an empty response with the original sinceDateTime when an error occurs or when
        ///     an unexpected HTTP status code is returned from the API.
        /// </remarks>
        Task<(ProductList ProductList, DateTime? LastModified)> GetProductVersionListAsync(DataStandard dataStandard, DateTime? sinceDateTime, Job job);

        /// <summary>
        ///     Retrieves product names and their details from the Sales Catalogue Service.
        /// </summary>
        /// <param name="dataStandard">The data standard.</param>
        /// <param name="productNames">A collection of product names to retrieve.</param>
        /// <param name="job">The job context for the request.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        ///     The response containing product details or an empty response if an error occurs.
        /// </returns>
        Task<ProductEditionList> GetProductEditionListAsync(DataStandard dataStandard, IEnumerable<ProductName> productNames, Job job, CancellationToken cancellationToken);

        /// <summary>
        ///     Retrieves product versions for a list of product names from the Sales Catalogue Service ProductVersions endpoint.
        /// </summary>
        /// <param name="dataStandard">The data standard.</param>
        /// <param name="productVersion">A collection of product version to retrieve.</param>
        /// <param name="job">The job context for the request.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        ///     - ProductEditionList: The product edition list
        /// </returns>
        Task<ProductEditionList> GetProductVersionsListAsync(DataStandard dataStandard, IEnumerable<ProductVersion> productVersion, Job job, CancellationToken cancellationToken);
    }
}
