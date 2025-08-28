namespace UKHO.ADDS.EFS.Domain.Builds.S57
{
    public class S57Build : Build
    {
        private List<string> _products;

        public S57Build()
        {
            _products = [];
        }

        public IEnumerable<string>? Products
        {
            get => _products;
            set => _products = value?.ToList() ?? [];
        }

        public override string GetProductDelimitedList() => (Products == null) ? string.Empty : string.Join(", ", Products.Select(p => p));

        public override string GetProductDiscriminant() => $"s57-{Guid.NewGuid():N}"; // TODO Implement when product list is available

        public override int GetProductCount() => (Products == null) ? 0 : Products?.Count() ?? 0;
    }
}
