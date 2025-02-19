using System.Collections.Generic;
using System.Threading.Tasks;

namespace UKHO.FileShareService
{
    public interface ISalesCatalogueService
    {
        public Task<Object> GetProductsFromSpecificDateAsync(string sinceDateTime, string correlationId);
        public Task<Object> PostProductIdentifiersAsync(List<string> productIdentifiers, string correlationId);
        public Task<Object> PostProductVersionsAsync(List<Object> productVersions, string correlationId);
        public Task<Object> GetSalesCatalogueDataResponse(string batchId, string correlationId);3



        public Task<Object> GetBasicCatalogue(string batchId, string correlationId);
    }
}
