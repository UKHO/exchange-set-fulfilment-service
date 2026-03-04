using System.Net.Mime;
using Azure.Data.AppConfiguration;
using UKHO.ADDS.Aspire.Configuration.Seeder.Json;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Aspire.Configuration.Seeder.Services
{
    internal class ConfigurationService
    {
        public async Task SeedConfigurationAsync(AddsEnvironment environment, ConfigurationClient configurationClient, string serviceName, string configFilePath, string servicesFilePath, CancellationToken cancellationToken)
        {
            var label = serviceName.ToLowerInvariant();

            var configurationSetting = new ConfigurationSetting(WellKnownConfigurationName.ReloadSentinelKey, "change this value to reload all", label) { ContentType = MediaTypeNames.Text.Plain };
            await configurationClient.SetConfigurationSettingAsync(configurationSetting, false, cancellationToken);

            var configJson = await File.ReadAllTextAsync(configFilePath, cancellationToken);
            var configJsonCleaned = JsonStripper.StripJsonComments(configJson);

            var flattenedConfig = JsonFlattener.Flatten(environment, configJsonCleaned, label);

            foreach (var value in flattenedConfig)
            {
                await configurationClient.SetConfigurationSettingAsync(value.Value, false, cancellationToken);
            }

            var externalServiceJson = await File.ReadAllTextAsync(servicesFilePath, cancellationToken);
            var externalServiceJsonCleaned = JsonStripper.StripJsonComments(externalServiceJson);

            var externalServices = await ExternalServiceDefinitionParser.ParseAndResolveAsync(environment, externalServiceJsonCleaned);

            foreach (var externalService in externalServices)
            {
                var json = JsonCodec.Encode(externalService);
                var key = $"{WellKnownConfigurationName.ExternalServiceKeyPrefix}:{externalService.Service}";

                configurationSetting = new ConfigurationSetting(key, json, label) { ContentType = MediaTypeNames.Text.Plain };
                await configurationClient.SetConfigurationSettingAsync(configurationSetting, false, cancellationToken);
            }
        }
    }
}
