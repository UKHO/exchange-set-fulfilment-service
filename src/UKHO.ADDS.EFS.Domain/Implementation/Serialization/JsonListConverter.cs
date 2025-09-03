using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.Domain.Implementation.Serialization
{
    /// <summary>
    ///     Typed converter that writes/reads the wrapper as a JSON array.
    /// </summary>
    /// <typeparam name="TWrapper">
    ///     The wrapper type (must implement IJsonListWrapper&lt;TElement&gt; and have a public parameterless ctor).
    /// </typeparam>
    /// <typeparam name="TElement">Element type.</typeparam>
    internal sealed class JsonListConverter<TWrapper, TElement> : JsonConverter<TWrapper> where TWrapper : class, IJsonList<TElement>, new()
    {
        public override TWrapper? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            // Support two input shapes:
            // 1) Bare array: [ ... ]
            // 2) Legacy object shape with a single array property: { "<anything>": [ ... ] }
            // We prefer (1), but auto-upgrade (2) for painless migration

            var wrapper = new TWrapper();

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                ReadArrayIntoWrapper(ref reader, wrapper, options);
                return wrapper;
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                // Legacy object shape: find first property that is an array and read it
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException("Expected StartObject or StartArray.");
                }

                JsonElement? arrayProp = null;

                foreach (var property in root.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        arrayProp = property.Value;
                        break;
                    }
                }

                if (arrayProp is null)
                {
                    // If no array property exists, treat as empty wrapper
                    return wrapper;
                }

                foreach (var element in arrayProp.Value.EnumerateArray())
                {
                    var item = element.Deserialize<TElement>(options);
                    wrapper.Add(item!);
                }

                return wrapper;
            }

            throw new JsonException("Expected a JSON array or object for list wrapper.");
        }

        public override void Write(Utf8JsonWriter writer, TWrapper value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartArray();

            foreach (var item in value.Items)
            {
                JsonSerializer.Serialize(writer, item, options);
            }

            writer.WriteEndArray();
        }

        private static void ReadArrayIntoWrapper(ref Utf8JsonReader reader, TWrapper wrapper, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected StartArray.");
            }

            // Clear just in case we are reusing instances (unlikely during normal deserialize)
            wrapper.Clear();

            var elementConverter = (JsonConverter<TElement>?)options.GetConverter(typeof(TElement));

            // Move to first element or EndArray
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return;
                }

                // Delegate item reading to runtime element converter to respect custom converters
                var item = elementConverter is not null
                    ? elementConverter.Read(ref reader, typeof(TElement), options)
                    : JsonSerializer.Deserialize<TElement>(ref reader, options);

                wrapper.Add(item!);
            }

            throw new JsonException("Incomplete array while reading list wrapper.");
        }
    }
}
