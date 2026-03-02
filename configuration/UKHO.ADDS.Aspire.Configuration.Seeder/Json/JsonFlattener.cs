using System.Text.Json;

namespace UKHO.ADDS.Aspire.Configuration.Seeder.Json
{
    internal class JsonFlattener
    {
        public static IDictionary<string, ConfigurationEntry> Flatten(AddsEnvironment environment, string json)
        {
            using var document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty(environment.ToString(), out var envElement))
            {
                throw new ArgumentException($"Environment '{environment}' not found in the JSON.");
            }

            var result = new Dictionary<string, ConfigurationEntry>();
            FlattenElement(envElement, string.Empty, result);
            return result;
        }

        private static void FlattenElement(JsonElement element, string prefix, IDictionary<string, ConfigurationEntry> result)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var newPrefix = string.IsNullOrEmpty(prefix)
                            ? property.Name
                            : $"{prefix}:{property.Name}";
                        FlattenElement(property.Value, newPrefix, result);
                    }

                    break;

                case JsonValueKind.Array:
                    var items = element.EnumerateArray().ToList();

                    if (items.Count == 2 && items[0].ValueKind == JsonValueKind.String && items[1].ValueKind == JsonValueKind.String)
                    {
                        // First element is the value, second element is the content type
                        result[prefix] = new ConfigurationEntry(items[0].ToString(), items[1].ToString());
                    }
                    else
                    {
                        var index = 0;
                        foreach (var item in items)
                        {
                            var newPrefix = $"{prefix}:{index}";
                            FlattenElement(item, newPrefix, result);
                            index++;
                        }
                    }

                    break;

                case JsonValueKind.String:
                    result[prefix] = new ConfigurationEntry(element.GetString()!);
                    break;

                case JsonValueKind.Number:
                    result[prefix] = new ConfigurationEntry(element.ToString());
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    result[prefix] = new ConfigurationEntry(element.GetBoolean().ToString());
                    break;

                case JsonValueKind.Null:
                    result[prefix] = new ConfigurationEntry("null");
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported JSON token: {element.ValueKind}");
            }
        }
    }
}
