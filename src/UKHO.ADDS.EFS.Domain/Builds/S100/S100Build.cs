using System.Text;
using UKHO.ADDS.EFS.Products;

namespace UKHO.ADDS.EFS.Builds.S100
{
    public class S100Build : Build
    {
        private List<ProductVersion> _products;
        private List<ProductEdition> _productNames;

        public S100Build()
        {
            _products = [];
            _productNames = [];
        }

        public IEnumerable<ProductVersion>? Products
        {
            get => _products;
            set => _products = value?.ToList() ?? [];
        }

        public IEnumerable<ProductEdition> ProductNames
        {
            get => _productNames;
            set => _productNames = value?.ToList() ?? [];
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
