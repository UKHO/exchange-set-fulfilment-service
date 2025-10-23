using System.Collections;
using System.Net;
using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.Domain.Implementation.Serialization;

namespace UKHO.ADDS.EFS.Domain.Products
{
    [JsonConverter(typeof(JsonListConverterFactory))]
    public sealed class ProductList : IJsonList<Product>, IReadOnlyList<Product>
    {
        private readonly List<Product> _products = [];

        // Expose as read-only; no public setter
        public IReadOnlyList<Product> Products => _products;

        public ProductCount Count => ProductCount.From(_products.Count);

        public bool HasProducts => _products.Count > 0;

        public DateTime? ProductsLastModified { get; set; }

        public bool Add(Product product)
        {
            if (product is null)
            {
                return false;
            }

            _products.Add(product);
            return true;
        }

        public bool Remove(Product product) => _products.Remove(product);

        public void Clear() => _products.Clear();

        public IEnumerator<Product> GetEnumerator() => _products.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // IReadOnlyList implementation
        int IReadOnlyCollection<Product>.Count => _products.Count;

        public Product this[int index] => _products[index];

        // IJsonList implementation
        void IJsonList<Product>.Add(Product item) => _products.Add(item);

        IReadOnlyList<Product> IJsonList<Product>.Items => _products;
    }
}
