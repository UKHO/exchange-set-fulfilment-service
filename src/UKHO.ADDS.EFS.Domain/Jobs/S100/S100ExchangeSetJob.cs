using UKHO.ADDS.Clients.SalesCatalogueService.Models;

namespace UKHO.ADDS.EFS.Jobs.S100
{
    public class S100ExchangeSetJob : ExchangeSetJob
    {
        private List<S100Products> _products;

        public S100ExchangeSetJob()
        {
            _products = [];
        }

        public IEnumerable<S100Products>? Products
        {
            get => _products;
            set => _products = value?.ToList() ?? [];
        }

        public override string GetProductDelimitedList() => _products.Any() ? string.Join(", ", _products.Select(p => p.ProductName)) : string.Empty;

        public override int GetProductCount() => _products.Count;
    }
}
