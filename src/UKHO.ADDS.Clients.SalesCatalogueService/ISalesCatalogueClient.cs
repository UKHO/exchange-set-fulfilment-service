using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.Clients.SalesCatalogueService
{
    public interface ISalesCatalogueClient
    {
        public Task<IResult<S100SalesCatalogueResponse>> GetS100ProductsFromSpecificDateAsync(string apiVersion, string productType, DateTime? sinceDateTime, string correlationId);
        public Task<IResult<SalesCatalogueResponse>> GetProductsFromSpecificDateAsync(string sinceDateTime, string correlationId);
        public Task<IResult<SalesCatalogueResponse>> PostProductIdentifiersAsync(List<string> productIdentifiers, string correlationId);
        public Task<IResult<SalesCatalogueResponse>> PostProductVersionsAsync(List<ProductVersionRequest> productVersions, string correlationId);
        public Task<IResult<SalesCatalogueDataResponse>> GetSalesCatalogueDataResponse(string batchId, string correlationId);
        public Task<IResult<S100ProductNamesResponse>> GetS100ProductNamesAsync(string apiVersion, string productType, IEnumerable<string> productNames, string correlationId, CancellationToken cancellationToken = default);
    }
}
