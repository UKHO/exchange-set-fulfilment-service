using System.Dynamic;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.Storage;
using AzureKeyVaultEmulator.Aspire.Hosting;
using HandlebarsDotNet;
using Microsoft.Extensions.Hosting;
using Projects;
using UKHO.ADDS.Configuration.Aspire.Extensions;
using UKHO.ADDS.Configuration.Extensions;
using UKHO.ADDS.Configuration.Schema;

namespace UKHO.ADDS.Configuration.Aspire
{
    public static class DistributedApplicationBuilderExtensions
    {
        public static IResourceBuilder<ProjectResource> AddConfiguration(this IDistributedApplicationBuilder builder, string configJsonPath, Action<EndpointTemplateBuilder> templateCallback, string externalServiceDiscoPath, IEnumerable<IResourceBuilder<ProjectResource>> externalServiceMocks, string? serviceNameTag = null)
        {
            var storage = builder.AddAzureStorage(WellKnownConfigurationName.ConfigurationServiceStorageName).RunAsEmulator(e => { e.WithDataVolume(); });

            if (!string.IsNullOrWhiteSpace(serviceNameTag))
            {
                storage.ConfigureInfrastructure(config =>
                {
                    var storageAccount = config.GetProvisionableResources().OfType<StorageAccount>().Single();
                    storageAccount.Tags.Add("hidden-title", serviceNameTag);
                });
            }

            var storageTable = storage.AddTables(WellKnownConfigurationName.ConfigurationServiceTableStorageName);
            var keyVault = builder.AddAzureKeyVaultEmulator(WellKnownConfigurationName.ConfigurationServiceKeyVaultName, new KeyVaultEmulatorOptions { Persist = false });

            if (!string.IsNullOrWhiteSpace(serviceNameTag))
            {
                keyVault.ConfigureInfrastructure(config =>
                {
                    var keyVaultService = config.GetProvisionableResources().OfType<KeyVaultService>().Single();
                    keyVaultService.Tags.Add("hidden-title", serviceNameTag);
                });
            }

            var configOriginalPath = Path.GetFullPath(configJsonPath);
            var configFilePath = CopyToTempFile(configOriginalPath);

            var configJsonRaw = File.ReadAllText(configFilePath);
            var configJson = configJsonRaw.StripJsonComments();


            var externalServiceDiscoOriginalPath = Path.GetFullPath(externalServiceDiscoPath);
            var externalServiceDiscoFilePath = CopyToTempFile(externalServiceDiscoOriginalPath);

            IResourceBuilder<ProjectResource> seederService = null!;

            if (builder.Environment.IsDevelopment())
            {
                var template = Handlebars.Compile(configJson);
                var context = new ExpandoObject();

                var templateBuilder = new EndpointTemplateBuilder();

                // Only add the seeder service in local development environment
                seederService = builder.AddProject<UKHO_ADDS_Configuration_Seeder>(WellKnownConfigurationName.ConfigurationSeederName)
                    .WithReference(storageTable)
                    .WaitFor(storageTable)
                    .WithEnvironment(x =>
                    {
                        x.EnvironmentVariables.Add(WellKnownConfigurationName.ConfigurationFilePath, configFilePath);
                        x.EnvironmentVariables.Add(WellKnownConfigurationName.ExternalServiceDiscoFilePath, externalServiceDiscoFilePath);

                        // TODO Remove after config upgrade

                        templateCallback(templateBuilder);

                        foreach (var endpoint in templateBuilder.Templates)
                        {
                            var url = endpoint.Resource.GetEndpoint(endpoint.UseHttps ? "https" : "http").Url;
                            var urlBuilder = new UriBuilder(url);

                            if (!string.IsNullOrEmpty(endpoint.Hostname))
                            {
                                urlBuilder.Host = endpoint.Hostname;
                            }

                            var path = endpoint.Path;

                            if (path != null && !path.EndsWith("/"))
                            {
                                path = string.Concat(path, "/");
                                urlBuilder.Path = path;
                            }

                            context.TryAdd(endpoint.Name, urlBuilder.ToString());
                        }

                        var resultJson = template(context);
                        File.WriteAllText(configFilePath, resultJson);
                    });

                foreach (var mock in externalServiceMocks)
                {
                    seederService.WithReference(mock);
                }
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

        private static string CopyToTempFile(string sourceFilePath)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            var content = File.ReadAllText(sourceFilePath);
            File.WriteAllText(tempFilePath, content);

            return tempFilePath;
        }
    }
}
