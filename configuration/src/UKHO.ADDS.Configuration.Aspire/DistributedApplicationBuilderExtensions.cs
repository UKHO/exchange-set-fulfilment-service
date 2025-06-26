using System.Dynamic;
using AzureKeyVaultEmulator.Aspire.Hosting;
using HandlebarsDotNet;
using Microsoft.Extensions.Hosting;
using Projects;
using UKHO.ADDS.Configuration.Aspire.Extensions;
using UKHO.ADDS.Configuration.Schema;

namespace UKHO.ADDS.Configuration.Aspire
{
    public static class DistributedApplicationBuilderExtensions
    {
        public static IResourceBuilder<ProjectResource> AddConfiguration(this IDistributedApplicationBuilder builder, string configJsonPath, Func<IEnumerable<(string name, IResourceBuilder<ProjectResource> resource)>> endpointsCallback)
        {
            var storage = builder.AddAzureStorage(WellKnownConfigurationName.ConfigurationServiceStorageName).RunAsEmulator(e => { e.WithDataVolume(); });

            var storageTable = storage.AddTables(WellKnownConfigurationName.ConfigurationServiceTableStorageName);
            var keyVault = builder.AddAzureKeyVaultEmulator(WellKnownConfigurationName.ConfigurationServiceKeyVaultName, new KeyVaultEmulatorOptions { Persist = false });

            var configFilePath = Path.GetFullPath(configJsonPath);

            var configJson = File.ReadAllText(configFilePath);

            IResourceBuilder<ProjectResource> seederService = null!;

            if (builder.Environment.IsDevelopment())
            {
                var template = Handlebars.Compile(configJson);
                var context = new ExpandoObject();
                

                // Only add the seeder service in local development environment
                seederService = builder.AddProject<ConfigurationSeeder>(WellKnownConfigurationName.ConfigurationSeederName)
                    .WithReference(storageTable)
                    .WithEnvironment(x =>
                    {
                        x.EnvironmentVariables.Add(WellKnownConfigurationName.ConfigurationFilePath, configFilePath);

                        var endpoints = endpointsCallback();

                        foreach (var endpoint in endpoints)
                        {
                            var url = endpoint.resource.GetEndpoint("http").Url;

                            context.TryAdd(endpoint.name, url);
                        }

                        var resultJson = template(context);
                        File.WriteAllText(configFilePath, resultJson);

                        //var fssBuilderEndpoint = new UriBuilder(addsMockEndpoint.Url) { Host = "host.docker.internal", Path = "fss/" };
                        //var fssOrchestratorEndpoint = new UriBuilder(addsMockEndpoint.Url) { Host = addsMockEndpoint.Host, Path = "fss/" };
                        //var scsEndpoint = new UriBuilder(addsMockEndpoint.Url) { Host = addsMockEndpoint.Host, Path = "scs/" };
                    });
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
