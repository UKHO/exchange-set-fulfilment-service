using System.Collections;
using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.Domain.Implementation.Serialization;

namespace UKHO.ADDS.EFS.Domain.Products
{
    [JsonConverter(typeof(JsonListConverterFactory))]
    public sealed class ProductNameList : IJsonList<ProductName>, IReadOnlyList<ProductName>
    {
        private readonly List<ProductName> _products = [];

        public IReadOnlyList<ProductName> Names => _products;

        public ProductCount Count => ProductCount.From(_products.Count);

        public bool HasProducts => _products.Count > 0;

        public bool Add(ProductName product)
        {
            if (_products.Contains(product))
            {
                return false;
            }

            _products.Add(product);
            return true;
        }

        public bool Remove(ProductName product) => _products.Remove(product);

        public void Clear() => _products.Clear();

        public IEnumerator<ProductName> GetEnumerator() => _products.GetEnumerator();

        public ProductName this[int index] => _products[index];

        void IJsonList<ProductName>.Add(ProductName item) => _products.Add(item);

        IReadOnlyList<ProductName> IJsonList<ProductName>.Items => _products;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        int IReadOnlyCollection<ProductName>.Count => _products.Count;
    }
}
