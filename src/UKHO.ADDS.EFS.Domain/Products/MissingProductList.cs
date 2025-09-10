using System.Collections;
using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.Domain.Implementation.Serialization;

namespace UKHO.ADDS.EFS.Domain.Products
{
    [JsonConverter(typeof(JsonListConverterFactory))]
    public sealed class MissingProductList : IJsonList<MissingProduct>, IReadOnlyList<MissingProduct>
    {
        private readonly List<MissingProduct> _products = [];

        // Expose as read-only; no public setter
        public IReadOnlyList<MissingProduct> Products => _products;

        public bool HasProducts => _products.Count > 0;

        // ProductCount to match other list wrappers
        public ProductCount Count => ProductCount.From(_products.Count);

        public bool Add(MissingProduct product)
        {
            _products.Add(product);
            return true;
        }

        public bool Remove(MissingProduct product) => _products.Remove(product);

        public void Clear() => _products.Clear();

        public IEnumerator<MissingProduct> GetEnumerator() => _products.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // IReadOnlyList implementation
        int IReadOnlyCollection<MissingProduct>.Count => _products.Count;

        public MissingProduct this[int index] => _products[index];

        // IJsonList implementation
        void IJsonList<MissingProduct>.Add(MissingProduct item) => _products.Add(item);

        IReadOnlyList<MissingProduct> IJsonList<MissingProduct>.Items => _products;
    }
}
