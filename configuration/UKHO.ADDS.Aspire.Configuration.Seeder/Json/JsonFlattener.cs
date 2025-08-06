using System.Text.Json;

namespace UKHO.ADDS.Aspire.Configuration.Seeder.Json
{
    internal class JsonFlattener
    {
        public static IDictionary<string, string> Flatten(AddsEnvironment environment, string json)
        {
            using var document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty(environment.ToString(), out var envElement))
            {
                throw new ArgumentException($"Environment '{environment}' not found in the JSON.");
            }

            var result = new Dictionary<string, string>();
            FlattenElement(envElement, string.Empty, result);
            return result;
        }

        private static void FlattenElement(JsonElement element, string prefix, IDictionary<string, string> result)
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
                    var index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        var newPrefix = $"{prefix}:{index}";
                        FlattenElement(item, newPrefix, result);
                        index++;
                    }

                    break;

                case JsonValueKind.String:
                    result[prefix] = element.GetString()!;
                    break;

                case JsonValueKind.Number:
                    result[prefix] = element.ToString();
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    result[prefix] = element.GetBoolean().ToString();
                    break;

                case JsonValueKind.Null:
                    result[prefix] = "null";
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported JSON token: {element.ValueKind}");
            }
        }
    }
}
