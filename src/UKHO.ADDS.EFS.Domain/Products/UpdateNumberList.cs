using System.Collections;
using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.Domain.Implementation.Serialization;

namespace UKHO.ADDS.EFS.Domain.Products
{
    [JsonConverter(typeof(JsonListConverterFactory))]
    public sealed class UpdateNumberList : IJsonList<UpdateNumber>, IReadOnlyList<UpdateNumber>
    {
        private readonly List<UpdateNumber> _items = [];

        public IReadOnlyList<UpdateNumber> Items => _items;

        public int Count => _items.Count;

        public bool HasItems => _items.Count > 0;

        public bool Add(UpdateNumber item)
        {
            _items.Add(item);
            return true;
        }

        public bool Remove(UpdateNumber item) => _items.Remove(item);

        public void Clear() => _items.Clear();

        public IEnumerator<UpdateNumber> GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // IReadOnlyList implementation
        public UpdateNumber this[int index] => _items[index];

        // IJsonList implementation
        void IJsonList<UpdateNumber>.Add(UpdateNumber item) => _items.Add(item);

        IReadOnlyList<UpdateNumber> IJsonList<UpdateNumber>.Items => _items;
    }
}
