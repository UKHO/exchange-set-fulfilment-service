using System.Net;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Domain.Products;

namespace UKHO.ADDS.EFS.Infrastructure.Adapters.Products
{
    /// <summary>
    ///     Adapter extensions to convert Sales Catalogue client models to domain models.
    /// </summary>
    public static class SalesCatalogueMappingExtensions
    {
        public static Product ToDomain(this S100BasicCatalogue source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            ProductStatus? parsedStatus = null;
            if (source.Status?.StatusDate != null)
            {
                parsedStatus = new ProductStatus
                {
                    StatusDate = source.Status.StatusDate.Value.DateTime, StatusName = source.Status.StatusName?.ToString() ?? string.Empty
                };
            }

            return new Product
            {
                ProductName = ProductName.From(source.ProductName!),
                LatestEditionNumber = source.LatestEditionNumber.HasValue
                    ? EditionNumber.From(source.LatestEditionNumber.Value)
                    : EditionNumber.NotSet,
                LatestUpdateNumber = source.LatestUpdateNumber.HasValue
                    ? UpdateNumber.From(source.LatestUpdateNumber.Value)
                    : UpdateNumber.NotSet,
                Status = parsedStatus
            };
        }

        public static ProductList ToDomain(this IEnumerable<S100BasicCatalogue> source, DateTime? lastModified)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var list = new ProductList
            {
                LastModified = lastModified ?? default,
                ResponseCode = HttpStatusCode.OK
            };

            foreach (var item in source)
            {
                list.Add(item.ToDomain());
            }

            return list;
        }

        public static ProductEdition ToDomain(this ProductNames source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var dates = source.Dates?.Select(d => new ProductDate
            {
                IssueDate = d.IssueDate?.DateTime ?? default, UpdateApplicationDate = d.UpdateApplicationDate?.DateTime ?? default, UpdateNumber = d.UpdateNumber.HasValue ? UpdateNumber.From(d.UpdateNumber.Value) : UpdateNumber.NotSet
            }).ToList() ?? [];

            ProductCancellation? cancellation = null;
            if (source.Cancellation is not null)
            {
                cancellation = new ProductCancellation
                {
                    EditionNumber = source.Cancellation.EditionNumber.HasValue
                        ? EditionNumber.From(source.Cancellation.EditionNumber.Value)
                        : EditionNumber.NotSet,
                    UpdateNumber = source.Cancellation.UpdateNumber.HasValue
                        ? UpdateNumber.From(source.Cancellation.UpdateNumber.Value)
                        : UpdateNumber.NotSet
                };
            }

            var edition = new ProductEdition
            {
                ProductName = ProductName.From(source.ProductName!),
                EditionNumber = source.EditionNumber.HasValue ? EditionNumber.From(source.EditionNumber.Value) : EditionNumber.NotSet,
                FileSize = source.FileSize ?? 0,
                Cancellation = cancellation
            };

            foreach (var d in dates)
            {
                edition.Dates.Add(d);
            }

            if (source.UpdateNumbers != null)
            {
                foreach (var n in source.UpdateNumbers.Where(i => i.HasValue).Select(i => i!.Value))
                {
                    edition.UpdateNumbers.Add(UpdateNumber.From(n));
                }
            }

            return edition;
        }

        public static List<ProductEdition> ToDomain(this IList<ProductNames>? source) => source?.Select(p => p.ToDomain()).ToList() ?? new List<ProductEdition>();

        public static ProductCountSummary? ToDomain(this S100ProductCounts? source)
        {
            if (source is null)
            {
                return null;
            }

            var summary = new ProductCountSummary
            {
                RequestedProductCount = source.RequestedProductCount.HasValue
                    ? ProductCount.From(source.RequestedProductCount.Value)
                    : ProductCount.None,
                ReturnedProductCount = source.ReturnedProductCount.HasValue
                    ? ProductCount.From(source.ReturnedProductCount.Value)
                    : ProductCount.None,
                RequestedProductsAlreadyUpToDateCount = source.RequestedProductsAlreadyUpToDateCount.HasValue
                    ? ProductCount.From(source.RequestedProductsAlreadyUpToDateCount.Value)
                    : ProductCount.None,
                MissingProducts = new MissingProductList()
            };

            foreach (var r in source.RequestedProductsNotReturned ?? [])
            {
                if (!Enum.TryParse(r.Reason?.ToString(), out MissingProductReason missingProductReason))
                {
                    missingProductReason = MissingProductReason.None;
                }

                summary.MissingProducts.Add(new MissingProduct
                {
                    ProductName = ProductName.From(r.ProductName!),
                    Reason = missingProductReason
                });
            }

            return summary;
        }

        public static ProductEditionList ToDomain(this S100ProductResponse source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var list = new ProductEditionList
            {
                ProductCountSummary = source.ProductCounts.ToDomain() ?? new ProductCountSummary(),
                ResponseCode = HttpStatusCode.OK
            };

            foreach (var p in source.Products ?? [])
            {
                list.Add(p.ToDomain());
            }

            return list;
        }
    }
}
