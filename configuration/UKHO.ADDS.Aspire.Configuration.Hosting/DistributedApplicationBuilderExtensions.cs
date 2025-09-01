using Aspire.Hosting.Azure;
using Projects;

namespace UKHO.ADDS.Aspire.Configuration.Hosting
{
    public static class DistributedApplicationBuilderExtensions
    {
        public static IResourceBuilder<AzureAppConfigurationResource> AddConfiguration(this IDistributedApplicationBuilder builder, string configurationName, IResourceBuilder<ParameterResource> addsEnvironment, IEnumerable<IResourceBuilder<ProjectResource>> configurationAwareProjects, bool publishAsExisting = false, IResourceBuilder<ParameterResource>? existingName = null, IResourceBuilder<ParameterResource>? exisitingResourceGroup = null)
        {
            var appConfig = publishAsExisting
                ? builder.AddAzureAppConfiguration(configurationName)
                : builder.AddAzureAppConfiguration(configurationName).PublishAsExisting(existingName!, exisitingResourceGroup);

            foreach (var project in configurationAwareProjects)
            {
                project.WithReference(appConfig);
                project.WithEnvironment(WellKnownConfigurationName.AddsEnvironmentName, addsEnvironment);
            }

            return appConfig;
        }

        public static IResourceBuilder<ProjectResource> AddConfigurationEmulator(this IDistributedApplicationBuilder builder, string serviceName, IEnumerable<IResourceBuilder<ProjectResource>> configurationAwareProjects, IEnumerable<IResourceBuilder<ProjectResource>> externalServiceMocks, string configJsonPath, string externalServicesPath)
        {
            var configResolvedPath = Path.GetFullPath(configJsonPath, builder.Environment.ContentRootPath);
            var configFilePath = CopyToTempFile(configResolvedPath);

            var externalServicesResolvedPath = Path.GetFullPath(externalServicesPath, builder.Environment.ContentRootPath);
            var externalServicesFilePath = CopyToTempFile(externalServicesResolvedPath);

            var emulator = builder.AddProject<UKHO_ADDS_Aspire_Configuration_Emulator>(WellKnownConfigurationName.ConfigurationServiceName)
                .WithEnvironment(WellKnownConfigurationName.AddsEnvironmentName, AddsEnvironment.Local.Value);

            // Only add the seeder service in local development environment
            var seederService = builder.AddProject<UKHO_ADDS_Aspire_Configuration_Seeder>(WellKnownConfigurationName.ConfigurationSeederName)
                .WithReference(emulator)
                .WaitFor(emulator)
                .WithEnvironment(x =>
                {
                    x.EnvironmentVariables.Add(WellKnownConfigurationName.AddsEnvironmentName, AddsEnvironment.Local.Value);

                    x.EnvironmentVariables.Add(WellKnownConfigurationName.ConfigurationFilePath, configFilePath);
                    x.EnvironmentVariables.Add(WellKnownConfigurationName.ExternalServicesFilePath, externalServicesFilePath);
                    x.EnvironmentVariables.Add(WellKnownConfigurationName.ServiceName, serviceName);
                });

            foreach (var mock in externalServiceMocks)
            {
                seederService.WithReference(mock);
            }

            foreach (var project in configurationAwareProjects)
            {
                project.WithReference(emulator);
                project.WaitFor(emulator);

                project.WaitFor(seederService);
                project.WithEnvironment(WellKnownConfigurationName.AddsEnvironmentName, AddsEnvironment.Local.Value);
            }

            return emulator;
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
