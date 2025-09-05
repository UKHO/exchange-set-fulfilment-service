using System.Collections;
using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.Domain.Implementation.Serialization;

namespace UKHO.ADDS.EFS.Domain.Files
{
    [JsonConverter(typeof(JsonListConverterFactory))]
    public sealed class AttributeList : IJsonList<Attribute>, IReadOnlyList<Attribute>
    {
        private readonly List<Attribute> _attributes = [];

        public IReadOnlyList<Attribute> Names => _attributes;

        public int Count => _attributes.Count;

        public bool HasAttributes => _attributes.Count > 0;

        public bool Add(Attribute product)
        {
            if (_attributes.Contains(product))
            {
                return false;
            }

            _attributes.Add(product);
            return true;
        }

        public bool Remove(Attribute product) => _attributes.Remove(product);

        public void Clear() => _attributes.Clear();

        public IEnumerator<Attribute> GetEnumerator() => _attributes.GetEnumerator();

        public Attribute this[int index] => _attributes[index];

        void IJsonList<Attribute>.Add(Attribute item) => _attributes.Add(item);

        IReadOnlyList<Attribute> IJsonList<Attribute>.Items => _attributes;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        int IReadOnlyCollection<Attribute>.Count => _attributes.Count;
    }
}
