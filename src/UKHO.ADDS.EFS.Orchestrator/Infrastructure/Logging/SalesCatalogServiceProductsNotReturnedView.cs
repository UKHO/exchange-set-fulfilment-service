using UKHO.ADDS.Clients.SalesCatalogueService.Models;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    public class SalesCatalogServiceProductsNotReturnedView
    {
        public int? RequestedProductCount { get; set; }

        public int? ReturnedProductCount { get; set; }

        public List<RequestedProductsNotReturned> RequestedProductsNotReturned { get; set; }

        public static SalesCatalogServiceProductsNotReturnedView Create(ProductCounts productCounts) =>
            new()
            {
                RequestedProductCount = productCounts.RequestedProductCount,
                ReturnedProductCount = productCounts.ReturnedProductCount,
                RequestedProductsNotReturned = productCounts.RequestedProductsNotReturned
            };
    }
}
