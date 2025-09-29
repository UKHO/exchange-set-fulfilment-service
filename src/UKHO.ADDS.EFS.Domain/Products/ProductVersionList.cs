using System.Collections;
using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.Domain.Implementation.Serialization;

namespace UKHO.ADDS.EFS.Domain.Products
{
    [JsonConverter(typeof(JsonListConverterFactory))]
    public sealed class ProductVersionList : IJsonList<ProductVersion>, IReadOnlyList<ProductVersion>
    {
        private readonly List<ProductVersion> _productsVersions = [];

        public IReadOnlyList<ProductVersion> ProductVersions => _productsVersions;

        public bool HasProducts => _productsVersions.Count > 0;

        public bool Add(ProductVersion productVersion)
        {
            if (_productsVersions.Contains(productVersion))
            {
                return false;
            }

            _productsVersions.Add(productVersion);
            return true;
        }

        public bool Remove(ProductVersion productVersion) => _productsVersions.Remove(productVersion);

        public void Clear() => _productsVersions.Clear();

        public IEnumerator<ProductVersion> GetEnumerator() => _productsVersions.GetEnumerator();

        public ProductVersion this[int index] => _productsVersions[index];

        void IJsonList<ProductVersion>.Add(ProductVersion item) => _productsVersions.Add(item);

        IReadOnlyList<ProductVersion> IJsonList<ProductVersion>.Items => _productsVersions;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        int IReadOnlyCollection<ProductVersion>.Count => _productsVersions.Count;
    }
}
