using AzureKeyVaultEmulator.Aspire.Hosting;
using Microsoft.AspNetCore.Builder;
using Projects;
using UKHO.ADDS.Configuration.Schema;

namespace UKHO.ADDS.Configuration.Aspire
{
    public static class DistributedApplicationBuilderExtensions
    {
        public static IResourceBuilder<ProjectResource> AddConfiguration(this IDistributedApplicationBuilder builder, string configJsonPath)
        {
            var storage = builder.AddAzureStorage(WellKnownConfigurationName.ConfigurationServiceStorageName).RunAsEmulator(e => { e.WithDataVolume(); });

            var storageTable = storage.AddTables(WellKnownConfigurationName.ConfigurationServiceTableStorageName);
            var keyVault = builder.AddAzureKeyVaultEmulator(WellKnownConfigurationName.ConfigurationServiceKeyVaultName, new KeyVaultEmulatorOptions { Persist = false });

            var configurationService = builder.AddProject<UKHO_ADDS_Configuration>(WellKnownConfigurationName.ConfigurationServiceName)
                .WithReference(storageTable)
                .WaitFor(storageTable)
                .WithReference(keyVault)
                .WaitFor(keyVault)
                .WithEnvironment(WellKnownConfigurationName.AddsEnvironmentName, AddsEnvironment.Local.Value);

            var configFilePath = Path.GetFullPath(configJsonPath);
            configurationService.WithEnvironment(WellKnownConfigurationName.ConfigurationFilePath, configFilePath);

            return configurationService;
        }

        public static IResourceBuilder<ProjectResource> WithConfiguration(this IResourceBuilder<ProjectResource> builder, IResourceBuilder<IResourceWithServiceDiscovery> service)
        {
            builder.WithReference(service);
            builder.WaitFor(service);
            builder.WithEnvironment(WellKnownConfigurationName.AddsEnvironmentName, AddsEnvironment.Local.Value);

            return builder;
        }
    }
}
