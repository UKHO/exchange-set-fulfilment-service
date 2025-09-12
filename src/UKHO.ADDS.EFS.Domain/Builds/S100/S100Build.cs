using System.Text;
using UKHO.ADDS.EFS.Domain.Products;

namespace UKHO.ADDS.EFS.Domain.Builds.S100
{
    public class S100Build : Build
    {
        private List<Product> _products;
        private List<ProductEdition> _productEditions;

        public S100Build()
        {
            _products = [];
            _productEditions = [];
        }

        public IEnumerable<Product>? Products
        {
            get => _products;
            set => _products = value?.ToList() ?? [];
        }

        public IEnumerable<ProductEdition> ProductEditions
        {
            get => _productEditions;
            set => _productEditions = value?.ToList() ?? [];
        }

        public override string GetProductDelimitedList() => (Products == null) ? string.Empty : string.Join(", ", Products.Select(p => p));

        public override string GetProductDiscriminant()
        {
            // TODO Produce a lexically ordered string for duplicate detection

            var builder = new StringBuilder("s100-");

            foreach (var product in Products!.OrderBy(p => p.ProductName).ThenBy(p => p.LatestEditionNumber).ThenBy(p => p.LatestUpdateNumber))
            {
                builder.Append(product.ProductName).Append(product.LatestEditionNumber).Append(product.LatestUpdateNumber).Append(product.Status);
            }

            return builder.ToString().ToLowerInvariant();
        }

        public override int GetProductCount() => (Products == null) ? 0 : Products?.Count() ?? 0;
    }
}
