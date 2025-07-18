namespace UKHO.ADDS.EFS.Builds.S63
{
    public class S63Build : Build
    {
        private List<string> _products;

        public S63Build()
        {
            _products = [];
        }

        public IEnumerable<string>? Products
        {
            get => _products;
            set => _products = value?.ToList() ?? [];
        }

        public override string GetProductDelimitedList() => (Products == null) ? string.Empty : string.Join(", ", Products.Select(p => p));

        public override string GetProductDiscriminant() => $"s63-{Guid.NewGuid():N}"; // TODO Implement when product list is available

        public override int GetProductCount() => (Products == null) ? 0 : Products?.Count() ?? 0;
    }
}
