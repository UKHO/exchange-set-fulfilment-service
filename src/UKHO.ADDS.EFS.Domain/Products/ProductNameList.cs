using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.Domain.Products
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

        public override string ToString() => string.Join(", ", _products.Select(p => p.ToString()));

        ////public ProductNameList(IEnumerable<string> products)
        ////{
        ////    _products = products?.Select(p => new ProductName(p)).ToList() ?? [];
        ////}

        //public ProductNameList(IEnumerable<ProductName> products)
        //{
        //    _products = products?.ToList() ?? [];
        //}

        ///// <summary>
        ///// Gets the products as an enumerable collection.
        ///// </summary>
        //public IEnumerable<ProductName> Products => _products;
    }
}
