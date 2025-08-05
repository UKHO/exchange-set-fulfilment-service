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

            var configJson = await File.ReadAllTextAsync(configFilePath, cancellationToken);
            var configJsonCleaned = JsonStripper.StripJsonComments(configJson);

            var flattenedConfig = JsonFlattener.Flatten(AddsEnvironment.Local, configJsonCleaned);

            await configurationClient.SetConfigurationSettingAsync(WellKnownConfigurationName.ReloadSentinelKey, "1", label, cancellationToken);

            foreach (var value in flattenedConfig)
            {
                await configurationClient.SetConfigurationSettingAsync(value.Key, value.Value, label, cancellationToken);
            }

            var externalServiceJson = await File.ReadAllTextAsync(servicesFilePath, cancellationToken);
            var externalServiceJsonCleaned = JsonStripper.StripJsonComments(externalServiceJson);

            var externalServices = await ExternalServiceDefinitionParser.ParseAndResolveAsync(environment, externalServiceJsonCleaned);

            foreach (var externalService in externalServices)
            {
                var json = JsonCodec.Encode(externalService);
                var key = $"{WellKnownConfigurationName.ExternalServiceKeyPrefix}:{externalService.Service}";

                await configurationClient.SetConfigurationSettingAsync(key, json, label, cancellationToken);
            }
        }
    }
}
