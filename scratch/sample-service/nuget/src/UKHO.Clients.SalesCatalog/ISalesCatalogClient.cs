using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.Infrastructure.Results;

namespace UKHO.Clients.SalesCatalog
{
    public interface ISalesCatalogClient
    {
        public Task<IResult<SalesCatalogueResponse>> GetProductsFromSpecificDateAsync(string sinceDateTime, string correlationId);

        public Task<IResult<SalesCatalogueResponse>> PostProductIdentifiersAsync(List<string> productIdentifiers, string correlationId);

        public Task<IResult<SalesCatalogueResponse>> PostProductVersionsAsync(List<ProductVersionRequest> productVersions, string correlationId);

        public Task<IResult<SalesCatalogueResponse>> GetSalesCatalogueDataResponse(string batchId, string correlationId);

        public Task<IResult<SalesCatalogueResponse>> GetBasicCatalogue(string batchId, string correlationId);
    }
}
