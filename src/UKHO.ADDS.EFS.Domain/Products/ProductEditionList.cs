using System.Collections;
using System.Net;
using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.Domain.Implementation.Serialization;

namespace UKHO.ADDS.EFS.Domain.Products
{
    [JsonConverter(typeof(JsonListConverterFactory))]
    public sealed class ProductEditionList : IJsonList<ProductEdition>, IReadOnlyList<ProductEdition>
    {
        private readonly List<ProductEdition> _products = new();

        public ProductCountSummary ProductCountSummary { get; set; } = new ProductCountSummary();

        // Expose as read-only; no public setter
        public IReadOnlyList<ProductEdition> Products => _products;

        public ProductCount Count => ProductCount.From(_products.Count);

        public bool HasProducts => _products.Count > 0;

        [JsonIgnore]
        public DateTime? ProductsLastModified { get; set; }

        public bool Add(ProductEdition product)
        {
            _products.Add(product);
            return true;
        }

        public bool Remove(ProductEdition product) => _products.Remove(product);

        public void Clear() => _products.Clear();

        public IEnumerator<ProductEdition> GetEnumerator() => _products.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // IReadOnlyList implementation
        int IReadOnlyCollection<ProductEdition>.Count => _products.Count;

        public ProductEdition this[int index] => _products[index];

        // IJsonList implementation
        void IJsonList<ProductEdition>.Add(ProductEdition item) => _products.Add(item);

        IReadOnlyList<ProductEdition> IJsonList<ProductEdition>.Items => _products;
    }
}
