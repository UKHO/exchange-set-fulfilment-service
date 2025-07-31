using Azure.Data.AppConfiguration;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UKHO.ADDS.Configuration.Schema;

namespace UKHO.ADDS.Configuration.Seeder
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                // Local environment

                var builder = Host.CreateApplicationBuilder(args);

                var configFilePath = builder.Configuration[WellKnownConfigurationName.ConfigurationFilePath]!;
                var newConfigFilePath = builder.Configuration[WellKnownConfigurationName.NewConfigurationFilePath]!;
                var discoFilePath = builder.Configuration[WellKnownConfigurationName.ExternalServiceDiscoFilePath]!;

                var servicePrefix = builder.Configuration[WellKnownConfigurationName.ServicePrefix]!;

                builder.AddAzureTableClient(WellKnownConfigurationName.ConfigurationServiceTableStorageName);
                builder.Services.AddSingleton<ConfigurationWriter>();
                builder.Services.AddSingleton<ExternalServiceDiscoWriter>();

                builder.Services.AddSingleton(x =>
                {
                    var serviceEnvironmentKey = $"services__{WellKnownConfigurationName.AzureConfigurationServiceName}__http__0";
                    var url = Environment.GetEnvironmentVariable(serviceEnvironmentKey)!;

                    var conStr = $"Endpoint={url};Id=abcd;Secret=c2VjcmV0;";
                    return new ConfigurationClient(conStr);
                });

                builder.Services.AddHostedService(x =>
                {
                    var configWriter = x.GetRequiredService<ConfigurationWriter>();
                    var discoWriter = x.GetRequiredService<ExternalServiceDiscoWriter>();

                    return new LocalSeederService(x.GetRequiredService<IHostApplicationLifetime>(), servicePrefix, x.GetRequiredService<ConfigurationClient>(), configWriter, discoWriter, configFilePath, newConfigFilePath, discoFilePath);
                });

                var app = builder.Build();

                await app.RunAsync();
            }
            else
            {
                // Deployed environment

                if (args.Length != 4)
                {
                    Console.WriteLine("Usage: <environment> <configFilePath> <discoFilePath> <tableUri>");
                    return 4;
                }

                var environmentName = args[0];
                Console.WriteLine($"Seeding configuration for environment: {environmentName}");
                var environment = AddsEnvironment.Parse(environmentName);

                var configFilePath = args[1];

                if (!File.Exists(configFilePath))
                {
                    Console.WriteLine($"Configuration file not found: {configFilePath}");
                    return 4;
                }

                Console.WriteLine($"Reading configuration from: {configFilePath}");
                var configJson = await File.ReadAllTextAsync(configFilePath);

                var discoFilePath = args[2];

                if (!File.Exists(discoFilePath))
                {
                    Console.WriteLine($"External service disco file not found: {discoFilePath}");
                    return 4;
                }

                Console.WriteLine($"Reading external service disco from: {discoFilePath}");
                var discoJson = await File.ReadAllTextAsync(discoFilePath);

                var tableUri = args[3];

                if (!Uri.TryCreate(tableUri, UriKind.Absolute, out var uri))
                {
                    Console.WriteLine($"Invalid Table Storage URI: {tableUri}");
                    return 4;
                }

                Console.WriteLine($"Using Table Storage URI: {uri}");

                var credential = new AzureCliCredential();
                var tableServiceClient = new TableServiceClient(uri, credential);

                var configurationWriter = new ConfigurationWriter(tableServiceClient);
                var discoWriter = new ExternalServiceDiscoWriter(tableServiceClient);

                var endpoints = await ExternalServiceDiscoParser.ParseAndResolveAsync(environment,  discoJson);
                await discoWriter.WriteExternalServiceDiscoAsync(endpoints);

                await configurationWriter.WriteConfigurationAsync(environment, configJson);
            }

            return 0;
        }
    }
}
