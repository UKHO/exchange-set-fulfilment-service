using System.Text;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;

namespace UKHO.ADDS.EFS.Builds.S100
{
    public class S100Build : Build
    {
        private List<S100Products> _products;
        private List<S100ProductNames> _s100ProductNames;

        public S100Build()
        {
            _products = [];
            _s100ProductNames = [];
        }

        public IEnumerable<S100Products>? Products
        {
            get => _products;
            set => _products = value?.ToList() ?? [];
        }

        public IEnumerable<S100ProductNames> S100ProductNames
        {
            get => _s100ProductNames;
            set => _s100ProductNames = value?.ToList() ?? [];
        }

        public override string GetProductDelimitedList() => (Products == null) ? string.Empty : string.Join(", ", Products.Select(p => p));

        public override string GetProductDiscriminator()
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
