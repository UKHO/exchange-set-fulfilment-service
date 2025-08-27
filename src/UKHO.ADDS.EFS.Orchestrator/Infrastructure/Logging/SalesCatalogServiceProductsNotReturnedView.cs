using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.VOS;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    public class SalesCatalogServiceProductsNotReturnedView
    {
        public ProductCount RequestedProductCount { get; init; }

        public ProductCount ReturnedProductCount { get; init; }

        public ProductCount RequestedProductsAlreadyUpToDateCount { get; init; }

        public List<RequestedProductsNotReturned> RequestedProductsNotReturned { get; init; }

        public static SalesCatalogServiceProductsNotReturnedView Create(ProductCounts productCounts) =>
            new()
            {
                RequestedProductCount = productCounts.RequestedProductCount,
                ReturnedProductCount = productCounts.ReturnedProductCount,
                RequestedProductsAlreadyUpToDateCount = productCounts.RequestedProductsAlreadyUpToDateCount,
                RequestedProductsNotReturned = productCounts.RequestedProductsNotReturned
            };
    }
}
