using UKHO.ADDS.Clients.SalesCatalogueService.Models;

namespace UKHO.ADDS.EFS.NewEFS.S100
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

        public override string GetProductDelimitedList() => throw new NotImplementedException();

        public override string GetProductDiscriminator() => throw new NotImplementedException();

        public override int GetProductCount() => throw new NotImplementedException();
    }
}
