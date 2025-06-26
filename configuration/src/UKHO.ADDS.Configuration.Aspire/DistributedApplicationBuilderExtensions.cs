using AzureKeyVaultEmulator.Aspire.Hosting;
using Microsoft.Extensions.Hosting;
using Projects;
using UKHO.ADDS.Configuration.Aspire.Extensions;
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

            var configFilePath = Path.GetFullPath(configJsonPath);

            IResourceBuilder<ProjectResource> seederService = null!;

            if (builder.Environment.IsDevelopment())
            {
                // Only add the seeder service in local development environment
                seederService = builder.AddProject<ConfigurationSeeder>(WellKnownConfigurationName.ConfigurationSeederName)
                    .WithReference(storageTable)
                    .WithEnvironment(WellKnownConfigurationName.ConfigurationFilePath, configFilePath);
            }

            var configurationService = builder.AddProject<UKHO_ADDS_Configuration>(WellKnownConfigurationName.ConfigurationServiceName)
                .WithReference(storageTable)
                .WaitFor(storageTable)
                .WithReference(keyVault)
                .WaitFor(keyVault)
                .WithScalar("API Browser")
                .WithDashboard("Configuration Dashboard")
                
                .WithEnvironment(WellKnownConfigurationName.AddsEnvironmentName, AddsEnvironment.Local.Value);

            if (seederService != null)
            {
                // Make sure the seeder runs before the configuration service starts
                configurationService.WithReference(seederService).WaitFor(seederService);
            }

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
