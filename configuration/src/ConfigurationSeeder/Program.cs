using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UKHO.ADDS.Configuration.Schema;

namespace ConfigurationSeeder
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                var builder = Host.CreateApplicationBuilder(args);

                var configFilePath = builder.Configuration[WellKnownConfigurationName.ConfigurationFilePath]!;

                builder.AddAzureTableClient(WellKnownConfigurationName.ConfigurationServiceTableStorageName);
                builder.Services.AddSingleton<ConfigurationWriter>();
                builder.Services.AddHostedService(x => new LocalSeederService(x.GetRequiredService<IHostApplicationLifetime>(),x.GetRequiredService<ConfigurationWriter>(), configFilePath));

                var app = builder.Build();

                await app.RunAsync();
            }
            else
            {
                var environmentName = args[0];
                var environment = AddsEnvironment.Parse(environmentName);

                var configFilePath = args[1]; 
                var configJson = await File.ReadAllTextAsync(configFilePath);

                var connectionString = args[2];
                var tableServiceClient = new TableServiceClient(connectionString);
                var configurationWriter = new ConfigurationWriter(tableServiceClient);

                await configurationWriter.WriteConfigurationAsync(environment, configJson);
            }

            return 0;
        }
    }
}
