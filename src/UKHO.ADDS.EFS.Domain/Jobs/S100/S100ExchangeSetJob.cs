using UKHO.ADDS.Clients.SalesCatalogueService.Models;

namespace UKHO.ADDS.EFS.Jobs.S100
{
    public class S100ExchangeSetJob : ExchangeSetJob
    {
        // TODO Check serialization (IEnumerable + setter)

        public List<S100Products>? Products { get; set; }

        public override string GetProductDelimitedList() => (Products == null) ? string.Empty : string.Join(", ", Products.Select(p => p.ProductName));
        public override int GetProductCount() => (Products == null) ? 0 : Products?.Count ?? 0;
        public List<S100ProductNames>? S100ProductNames { get; set; }
    }
}
