using UKHO.ADDS.Clients.SalesCatalogueService.Models;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    public class SalesCatalogServiceProductsNotReturnedView
    {
        public int? RequestedProductCount { get; init; }

        public int? ReturnedProductCount { get; init; }

        public int? RequestedProductsAlreadyUpToDateCount { get; init; }

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
