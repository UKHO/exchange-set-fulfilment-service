using System.Text.Json.Nodes;

namespace UKHO.ADDS.Configuration.Schema
{
    internal static class ConfigurationParser
    {
        public static List<EnvironmentConfiguration> Parse(string json)
        {
            var root = JsonNode.Parse(json)?.AsObject() ?? throw new InvalidOperationException("Invalid JSON");

            var schema = ParseGroupedSchema(root["_schema"]?.AsObject());
            var results = new List<EnvironmentConfiguration>();

            foreach (var (envKey, envNode) in root)
            {
                if (string.Equals(envKey, "_schema", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!AddsEnvironment.TryParse(envKey, out var environment))
                {
                    throw new InvalidOperationException($"Unknown environment: {envKey}");
                }

                var services = new List<ServiceConfiguration>();

                foreach (var (serviceName, serviceNode) in envNode!.AsObject())
                {
                    var flattened = new Dictionary<string, FlattenedProperty>(StringComparer.OrdinalIgnoreCase);
                    Flatten(string.Empty, serviceNode!, flattened);

                    if (schema.TryGetValue(serviceName, out var serviceSchema))
                    {
                        foreach (var (path, meta) in serviceSchema)
                        {
                            if (flattened.TryGetValue(path, out var existing))
                            {
                                ValidateType(path, existing.JsonValue, meta.Type, meta.Required);

                                flattened[path] = new FlattenedProperty { JsonValue = existing.JsonValue, Type = meta.Type ?? existing.Type, Required = meta.Required || existing.Required, Secret = meta.Secret || existing.Secret };
                            }
                        }
                    }

                    services.Add(new ServiceConfiguration { ServiceName = serviceName, Properties = flattened });
                }

                results.Add(new EnvironmentConfiguration { Environment = environment!, Services = services });
            }

            var allPropertyPaths = results
                .SelectMany(env => env.Services)
                .GroupBy(svc => svc.ServiceName)
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(svc => svc.Properties.Keys).ToHashSet(StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var (serviceName, properties) in schema)
            {
                if (!allPropertyPaths.TryGetValue(serviceName, out var definedPaths))
                {
                    throw new InvalidOperationException($"Schema references unknown service: '{serviceName}'");
                }

                var unused = properties.Keys
                    .Where(path => !definedPaths.Contains(path))
                    .ToList();

                if (unused.Count > 0)
                {
                    var formatted = string.Join(", ", unused);
                    throw new InvalidOperationException($"Schema for service '{serviceName}' references unknown properties: {formatted}");
                }
            }

            return results;
        }

        private static Dictionary<string, Dictionary<string, SchemaMetadata>> ParseGroupedSchema(JsonObject? node)
        {
            if (node is null)
            {
                return new Dictionary<string, Dictionary<string, SchemaMetadata>>();
            }

            var result = new Dictionary<string, Dictionary<string, SchemaMetadata>>(StringComparer.OrdinalIgnoreCase);

            foreach (var (serviceName, serviceSchemaNode) in node)
            {
                var serviceDict = new Dictionary<string, SchemaMetadata>(StringComparer.OrdinalIgnoreCase);
                if (serviceSchemaNode is not JsonObject serviceObject)
                {
                    throw new InvalidOperationException($"Schema entry for '{serviceName}' is not an object.");
                }

                foreach (var (path, metadataNode) in serviceObject)
                {
                    if (metadataNode is not JsonObject meta)
                    {
                        throw new InvalidOperationException($"Schema metadata for '{serviceName}:{path}' is not a valid object.");
                    }

                    serviceDict[path] = new SchemaMetadata { Type = meta["type"]?.GetValue<string>(), Required = meta["required"]?.GetValue<bool>() ?? false, Secret = meta["secret"]?.GetValue<bool>() ?? false };
                }

                result[serviceName] = serviceDict;
            }

            return result;
        }

        private static void Flatten(string prefix, JsonNode node, Dictionary<string, FlattenedProperty> output)
        {
            if (node is JsonObject obj)
            {
                foreach (var (key, child) in obj)
                {
                    var childPrefix = string.IsNullOrEmpty(prefix) ? key : $"{prefix}:{key}";
                    Flatten(childPrefix, child!, output);
                }
            }
            else if (node is JsonArray array)
            {
                output[prefix] = new FlattenedProperty { JsonValue = array, Type = null, Required = false, Secret = false };
            }
            else
            {
                output[prefix] = new FlattenedProperty { JsonValue = node, Type = null, Required = false, Secret = false };
            }
        }

        private static void ValidateType(string path, JsonNode? value, string? type, bool required)
        {
            if (required && (value is null || (value is JsonValue val && val.TryGetValue<string>(out var str) && string.IsNullOrWhiteSpace(str))))
            {
                throw new InvalidOperationException($"Required property '{path}' is null or empty.");
            }

            if (value is null || string.IsNullOrWhiteSpace(type))
            {
                return;
            }

            if (type.StartsWith("array:", StringComparison.OrdinalIgnoreCase))
            {
                if (value is not JsonArray array)
                {
                    throw new InvalidOperationException($"Type mismatch at '{path}': expected array, got {value.GetType().Name}");
                }

                var elementType = type.Substring("array:".Length).ToLowerInvariant();

                for (var i = 0; i < array.Count; i++)
                {
                    var element = array[i];
                    var elementPath = $"{path}[{i}]";
                    if (!ElementTypeMatches(element, elementType))
                    {
                        throw new InvalidOperationException($"Type mismatch at '{elementPath}': expected '{elementType}', actual value = '{element}'");
                    }
                }
            }
            else
            {
                var typeMatch = type.ToLowerInvariant() switch
                {
                    "string" => value is JsonValue v && v.TryGetValue<string>(out _),
                    "int" => value is JsonValue v && (v.TryGetValue<int>(out _) || v.TryGetValue<long>(out _)),
                    "double" => value is JsonValue v && (v.TryGetValue<double>(out _) || v.TryGetValue<float>(out _)),
                    "bool" => value is JsonValue v && v.TryGetValue<bool>(out _),
                    "datetime" => DateTime.TryParse(value.ToString(), out _),
                    "timespan" => TimeSpan.TryParse(value.ToString(), out _),
                    "url" => Uri.TryCreate(value.ToString(), UriKind.Absolute, out _),
                    _ => false
                };

                if (!typeMatch)
                {
                    throw new InvalidOperationException($"Type mismatch at '{path}': expected '{type}', actual value = '{value}'");
                }
            }
        }

        private static bool ElementTypeMatches(JsonNode? element, string type) =>
            type switch
            {
                "string" => element is JsonValue v && v.TryGetValue<string>(out _),
                "int" => element is JsonValue v && (v.TryGetValue<int>(out _) || v.TryGetValue<long>(out _)),
                "double" => element is JsonValue v && (v.TryGetValue<double>(out _) || v.TryGetValue<float>(out _)),
                "bool" => element is JsonValue v && v.TryGetValue<bool>(out _),
                "datetime" => DateTime.TryParse(element?.ToString(), out _),
                "timespan" => TimeSpan.TryParse(element?.ToString(), out _),
                "url" => Uri.TryCreate(element?.ToString(), UriKind.Absolute, out _),
                _ => false
            };
    }
}
