using System.Collections;
using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.Domain.Implementation.Serialization;

namespace UKHO.ADDS.EFS.Domain.Products
{
    [JsonConverter(typeof(JsonListConverterFactory))]
    public sealed class ProductDateList : IJsonList<ProductDate>, IReadOnlyList<ProductDate>
    {
        private readonly List<ProductDate> _items = [];

        public IReadOnlyList<ProductDate> Items => _items;

        public ProductCount Count => ProductCount.From(_items.Count);

        public bool HasItems => _items.Count > 0;

        public bool Add(ProductDate item)
        {
            _items.Add(item);
            return true;
        }

        public bool Remove(ProductDate item) => _items.Remove(item);

        public void Clear() => _items.Clear();

        public IEnumerator<ProductDate> GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // IReadOnlyList implementation
        int IReadOnlyCollection<ProductDate>.Count => _items.Count;

        public ProductDate this[int index] => _items[index];

        // IJsonList implementation
        void IJsonList<ProductDate>.Add(ProductDate item) => _items.Add(item);

        IReadOnlyList<ProductDate> IJsonList<ProductDate>.Items => _items;
    }
}
