using UKHO.Clients.Common.Configuration;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.Infrastructure.Results;

namespace UKHO.Clients.SalesCatalog
{
    internal class DummySalesCatalogClient : ISalesCatalogClient
    {
        public DummySalesCatalogClient(ClientConfiguration configuration)
        {
        }

        public async Task<IResult<SalesCatalogueResponse>> GetProductsFromSpecificDateAsync(string sinceDateTime, string correlationId)
        {
            return Result.Success(new SalesCatalogueResponse());
        }

        public async Task<IResult<SalesCatalogueResponse>> PostProductIdentifiersAsync(List<string> productIdentifiers, string correlationId)
        {
            return Result.Success(new SalesCatalogueResponse());
        }

        public async Task<IResult<SalesCatalogueResponse>> PostProductVersionsAsync(List<ProductVersionRequest> productVersions, string correlationId)
        {
            return Result.Success(new SalesCatalogueResponse());
        }

        public async Task<IResult<SalesCatalogueResponse>> GetSalesCatalogueDataResponse(string batchId, string correlationId)
        {
            return Result.Success(new SalesCatalogueResponse());
        }

        public async Task<IResult<SalesCatalogueResponse>> GetBasicCatalogue(string batchId, string correlationId)
        {
            return Result.Success(new SalesCatalogueResponse());
        }
    }
}
