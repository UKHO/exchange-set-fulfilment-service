using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.NewEFS.S100;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure
{
    internal interface IOrchestratorSalesCatalogueClient
    {
        /// <summary>
        ///     Retrieves S100 products that have been modified since a specific date.
        /// </summary>
        /// <param name="sinceDateTime">Optional date and time to filter products that have changed since this time.</param>
        /// <param name="build">The build</param>
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
        Task<(S100SalesCatalogueResponse s100SalesCatalogueData, DateTime? LastModified)> GetS100ProductsFromSpecificDateAsync(DateTime? sinceDateTime, S100Build build);
    }
}
