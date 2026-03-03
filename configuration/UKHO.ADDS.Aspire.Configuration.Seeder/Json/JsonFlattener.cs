using System.Text.Json;
using Azure.Data.AppConfiguration;

namespace UKHO.ADDS.Aspire.Configuration.Seeder.Json
{
    internal class JsonFlattener
    {
        public static IDictionary<string, ConfigurationSetting> Flatten(AddsEnvironment environment, string json, string label)
        {
            using var document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty(environment.ToString(), out var envElement))
            {
                throw new ArgumentException($"Environment '{environment}' not found in the JSON.");
            }

            var result = new Dictionary<string, ConfigurationSetting>();
            FlattenElement(envElement, string.Empty, result, label);
            return result;
        }

        private static void FlattenElement(JsonElement element, string prefix, IDictionary<string, ConfigurationSetting> result, string label)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var newPrefix = string.IsNullOrEmpty(prefix)
                            ? property.Name
                            : $"{prefix}:{property.Name}";
                        FlattenElement(property.Value, newPrefix, result, label);
                    }

                    break;

                case JsonValueKind.Array:
                    var items = element.EnumerateArray().ToList();

                    if (items.Count == 2 && items[0].ValueKind == JsonValueKind.String && items[1].ValueKind == JsonValueKind.String)
                    {
                        // First element is the value, second element is the content type
                        result[prefix] = new ConfigurationSetting(prefix, items[0].ToString(), label) { ContentType = items[1].ToString() };
                    }
                    else
                    {
                        var index = 0;
                        foreach (var item in items)
                        {
                            var newPrefix = $"{prefix}:{index}";
                            FlattenElement(item, newPrefix, result, label);
                            index++;
                        }
                    }

                    break;

                case JsonValueKind.String:
                    result[prefix] = new ConfigurationSetting(prefix, element.GetString()!, label);
                    break;

                case JsonValueKind.Number:
                    result[prefix] = new ConfigurationSetting(prefix, element.ToString(), label);
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    result[prefix] = new ConfigurationSetting(prefix, element.GetBoolean().ToString(), label);
                    break;

                case JsonValueKind.Null:
                    result[prefix] = new ConfigurationSetting(prefix, "null", label);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported JSON token: {element.ValueKind}");
            }
        }
    }
}
