using Azure.Data.AppConfiguration;

namespace UKHO.ADDS.Configuration.Seeder
{
    internal static class AzureConfigurationWriter
    {
        public static async Task WriteConfiguration(string prefix, ConfigurationClient client, IDictionary<string, string> values, IEnumerable<DiscoEndpointTemplate> externalServices)
        {
            foreach (var value in values)
            {
                await client.SetConfigurationSettingAsync(value.Key, value.Value);
            }

            foreach (var endpoint in externalServices)
            {
                await client.SetConfigurationSettingAsync($"{prefix}:.extdisco:{endpoint.Key}", endpoint.ResolvedUrl);
            }
        }
    }
}
