using System.Text.Json;
using System.Text.RegularExpressions;
using HandlebarsDotNet;
using UKHO.ADDS.Aspire.Configuration.Remote;

namespace UKHO.ADDS.Aspire.Configuration.Seeder.Json
{
    internal static class ExternalServiceDefinitionParser
    {
        private static readonly Regex _schemeRegex = new(@"^(?<scheme>https?)://", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex _placeholderRegex = new("{{(.*?)}}", RegexOptions.Compiled);

        public static async Task<List<ExternalServiceDefinition>> ParseAndResolveAsync(AddsEnvironment environment, string json)
        {
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty(environment.ToString(), out var envObject))
            {
                throw new InvalidDataException($"Missing '{environment}' section in discovery JSON");
            }

            var results = new List<ExternalServiceDefinition>();

            foreach (var serviceProp in envObject.EnumerateObject())
            {
                var serviceName = serviceProp.Name;
                var serviceObject = serviceProp.Value;

                if (!serviceObject.TryGetProperty("endpoints", out var endpointsArray) || endpointsArray.GetArrayLength() == 0)
                {
                    throw new InvalidDataException($"Service '{serviceName}' must have at least one endpoint.");
                }

                if (!serviceObject.TryGetProperty("clientId", out var clientIdElement))
                {
                    throw new InvalidDataException($"Service '{serviceName}' is missing required 'clientId' field.");
                }

                var clientId = clientIdElement.GetString() ?? throw new InvalidDataException($"clientId for '{serviceName}' is null");

                var serviceEndpoints = new List<ExternalEndpointTemplate>();
                var hasDefaultTag = false;

                foreach (var endpoint in endpointsArray.EnumerateArray())
                {
                    var template = endpoint.GetProperty("url").GetString() ?? throw new InvalidDataException($"Missing 'url' in endpoint of '{serviceName}'");
                    var tag = endpoint.GetProperty("tag").GetString() ?? "";

                    if (string.IsNullOrWhiteSpace(tag))
                    {
                        hasDefaultTag = true;
                    }

                    var schemeMatch = _schemeRegex.Match(template);
                    if (!schemeMatch.Success)
                    {
                        throw new InvalidDataException($"Invalid or missing scheme in URL: {template}");
                    }

                    var scheme = schemeMatch.Groups["scheme"].Value.ToLowerInvariant();

                    string resolvedUrl;
                    string? placeholder = null;

                    var placeholderMatches = _placeholderRegex.Matches(template);
                    if (environment == AddsEnvironment.Local && placeholderMatches.Count > 1)
                    {
                        throw new InvalidDataException($"Template for service '{serviceName}' contains more than one placeholder, which is not supported: {template}");
                    }

                    if (environment == AddsEnvironment.Local && placeholderMatches.Count == 1)
                    {
                        placeholder = placeholderMatches[0].Groups[1].Value;
                        var fullUrl = await LookupEndpointAsync(scheme, placeholder);
                        var uri = new Uri(fullUrl);

                        var hostAndPort = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";

                        var compiled = Handlebars.Compile(template);
                        resolvedUrl = compiled(new Dictionary<string, string>
                        {
                            [placeholder] = hostAndPort
                        });
                    }
                    else
                    {
                        resolvedUrl = template;
                    }

                    serviceEndpoints.Add(new ExternalEndpointTemplate
                    {
                        Service = serviceName,
                        Tag = tag,
                        Scheme = scheme,
                        OriginalTemplate = template,
                        Placeholder = placeholder,
                        ResolvedUrl = resolvedUrl
                    });
                }

                if (!hasDefaultTag)
                {
                    throw new InvalidDataException($"Service '{serviceName}' must have at least one endpoint with tag=\"\" (the default).");
                }

                results.Add(new ExternalServiceDefinition
                {
                    Service = serviceName,
                    Endpoints = serviceEndpoints,
                    ClientId = clientId
                });
            }

            return results;
        }

        private static Task<string> LookupEndpointAsync(string scheme, string serviceName)
        {
            var resolvedEndpointKey = $"services__{serviceName}__{scheme}__0";
            var resolvedEndpoint = Environment.GetEnvironmentVariable(resolvedEndpointKey);

            if (string.IsNullOrEmpty(resolvedEndpoint))
            {
                throw new InvalidOperationException($"Cannot find reference for service {serviceName} and scheme {scheme} in the environment. Did you reference it in the Aspire startup?");
            }

            return Task.FromResult(resolvedEndpoint);
        }
    }
}

