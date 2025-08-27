using System.Net;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Products;

namespace UKHO.ADDS.EFS.Domain.Adapters.Products
{
    /// <summary>
    ///     Adapter extensions to convert Sales Catalogue client models to domain models.
    /// </summary>
    public static class SalesCatalogueMappingExtensions
    {
        public static ProductVersion ToDomain(this S100BasicCatalogue source)
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

            return new ProductVersion
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

        public static ProductVersionList ToDomain(this IEnumerable<S100BasicCatalogue> source, DateTime? lastModified)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new ProductVersionList
            {
                ResponseBody = source.Select(x => x.ToDomain()).ToList(), LastModified = lastModified ?? default, ResponseCode = HttpStatusCode.OK
            };
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
            }).ToList() ?? new List<ProductDate>();

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

            return new ProductEdition
            {
                ProductName = ProductName.From(source.ProductName!),
                EditionNumber = source.EditionNumber.HasValue ? EditionNumber.From(source.EditionNumber.Value) : EditionNumber.NotSet,
                UpdateNumbers = source.UpdateNumbers != null
                    ? source.UpdateNumbers.Where(i => i.HasValue).Select(i => i!.Value).ToList()
                    : new List<int>(),
                Dates = dates,
                FileSize = source.FileSize ?? 0,
                Cancellation = cancellation
            };
        }

        public static List<ProductEdition> ToDomain(this IList<ProductNames>? source) => source?.Select(p => p.ToDomain()).ToList() ?? new List<ProductEdition>();

        public static ProductCountSummary? ToDomain(this S100ProductCounts? source)
        {
            if (source is null)
            {
                return null;
            }

            return new ProductCountSummary
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
                MissingProducts = source.RequestedProductsNotReturned?.Select(r => new MissingProduct
                {
                    ProductName = ProductName.From(r.ProductName!), Reason = r.Reason?.ToString() ?? string.Empty
                }).ToList() ?? new List<MissingProduct>()
            };
        }

        public static ProductEditionList ToDomain(this S100ProductResponse source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new ProductEditionList
            {
                Products = source.Products.ToDomain(), ProductCountSummary = source.ProductCounts.ToDomain(), ResponseCode = HttpStatusCode.OK
            };
        }
    }
}
