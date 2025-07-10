using UKHO.ADDS.Clients.SalesCatalogueService.Models;

namespace UKHO.ADDS.EFS.Builds.S100
{
    public class S100Build : Build
    {
        private List<S100Products> _products;

        public S100Build()
        {
            _products = [];
        }

        public IEnumerable<S100Products>? Products
        {
            get => _products;
            set => _products = value?.ToList() ?? [];
        }

        public override string GetProductDelimitedList() => (Products == null) ? string.Empty : string.Join(", ", Products.Select(p => p));

        public override string GetProductDiscriminator() => throw new NotImplementedException();

        public override int GetProductCount() => (Products == null) ? 0 : Products?.Count() ?? 0;
    }
}
