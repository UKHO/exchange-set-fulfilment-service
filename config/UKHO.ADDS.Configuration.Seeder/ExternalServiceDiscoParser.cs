using System.Text.Json;
using System.Text.RegularExpressions;
using HandlebarsDotNet;
using UKHO.ADDS.Configuration.Schema;

namespace UKHO.ADDS.Configuration.Seeder
{
    public static class ExternalServiceDiscoParser
    {
        private static readonly Regex _schemeRegex = new(@"^(?<scheme>https?)://", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex _placeholderRegex = new("{{(.*?)}}", RegexOptions.Compiled);

        public static async Task<List<DiscoEndpointTemplate>> ParseAndResolveAsync(AddsEnvironment environment, string json)
        {
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty(environment.ToString(), out var local))
            {
                throw new InvalidDataException($"Missing '{environment}' section in discovery JSON");
            }

            var results = new List<DiscoEndpointTemplate>();

            foreach (var property in local.EnumerateObject())
            {
                var key = property.Name;
                var template = property.Value.GetString() ?? string.Empty;

                var schemeMatch = _schemeRegex.Match(template);
                if (!schemeMatch.Success)
                {
                    throw new InvalidDataException($"Invalid or missing scheme in URL: {template}");
                }

                var scheme = schemeMatch.Groups["scheme"].Value.ToLowerInvariant();

                var placeholderMatches = _placeholderRegex.Matches(template);
                if (placeholderMatches.Count > 1)
                {
                    throw new InvalidDataException($"Template for key '{key}' contains more than one placeholder, which is not supported: {template}");
                }

                string resolvedUrl;
                string? placeholder = null;

                if (placeholderMatches.Count == 1)
                {
                    placeholder = placeholderMatches[0].Groups[1].Value;

                    var fullUrl = await LookupEndpointAsync(scheme, placeholder);
                    var uri = new Uri(fullUrl);

                    var hostAndPort = uri.IsDefaultPort
                        ? uri.Host
                        : $"{uri.Host}:{uri.Port}";

                    var compiled = Handlebars.Compile(template);
                    resolvedUrl = compiled(new Dictionary<string, string>
                    {
                        [placeholder] = hostAndPort
                    });
                }
                else
                {
                    // No template — use verbatim
                    resolvedUrl = template;
                }

                results.Add(new DiscoEndpointTemplate
                {
                    Key = key,
                    Scheme = scheme,
                    OriginalTemplate = template,
                    Placeholder = placeholder,
                    ResolvedUrl = resolvedUrl
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
