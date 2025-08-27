using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.VOS
{
    public sealed class ProductNameList
    {
        [JsonInclude] [JsonPropertyName("names")] private List<ProductName> _products = [];

        [JsonIgnore] public IReadOnlyList<ProductName> Names => _products;

        [JsonIgnore] public ProductCount Count => ProductCount.From(_products.Count);

        [JsonIgnore] public bool HasProducts => _products.Count > 0;

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
    }
}
