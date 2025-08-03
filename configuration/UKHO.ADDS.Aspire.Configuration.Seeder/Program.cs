using Azure.Data.AppConfiguration;
using Azure.Identity;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UKHO.ADDS.Aspire.Configuration.Seeder.Services;

namespace UKHO.ADDS.Aspire.Configuration.Seeder
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var sentinel = Environment.GetEnvironmentVariable(WellKnownConfigurationName.ConfigurationFilePath);

            if (string.IsNullOrEmpty(sentinel))
            {
                var environment = AddsEnvironment.GetEnvironment();

                var parseResult = Parser.Default.ParseArguments<CommandLineParameters>(args);

                if (parseResult.Value == null)
                {
                    return -1;
                }

                var parameters = parseResult.Value;

                try
                {
                    ValidateFilePath(parameters.ConfigurationFilePath, nameof(parameters.ConfigurationFilePath));
                    ValidateUri(parameters.AppConfigServiceUrl, nameof(parameters.AppConfigServiceUrl));

                    var configService = new ConfigurationService();
                    var configClient = new ConfigurationClient(new Uri(parameters.AppConfigServiceUrl), new DefaultAzureCredential());

                    await configService.SeedConfigurationAsync(
                        environment,
                        configClient,
                        parameters.ServiceName,
                        parameters.ConfigurationFilePath,
                        parameters.ServicesFilePath,
                        CancellationToken.None);

                    return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return -1;
                }
            }
            else
            {
                var environment = AddsEnvironment.GetEnvironment();

                if (environment.IsLocal())
                {
                    var builder = Host.CreateApplicationBuilder(args);

                    var configFilePath = builder.Configuration[WellKnownConfigurationName.ConfigurationFilePath]!;
                    var serviceFilePath = builder.Configuration[WellKnownConfigurationName.ExternalServicesFilePath]!;

                    var servicePrefix = builder.Configuration[WellKnownConfigurationName.ServicePrefix]!;

                    builder.Services.AddSingleton<ConfigurationService>();

                    builder.Services.AddSingleton(x =>
                    {
                        var serviceEnvironmentKey = $"services__{WellKnownConfigurationName.ConfigurationServiceName}__http__0";
                        var url = Environment.GetEnvironmentVariable(serviceEnvironmentKey)!;

                        var conStr = $"Endpoint={url};Id=aac-credential;Secret=c2VjcmV0;";
                        return new ConfigurationClient(conStr);
                    });

                    builder.Services.AddHostedService(x =>
                    {
                        var hostedLifetime = x.GetRequiredService<IHostApplicationLifetime>();
                        var configService = x.GetRequiredService<ConfigurationService>();

                        return new LocalSeederService(hostedLifetime, configService, servicePrefix, x.GetRequiredService<ConfigurationClient>(), configFilePath, serviceFilePath);
                    });

                    var app = builder.Build();

                    await app.RunAsync();
                }

                // We are running from Aspire
                return 0;
            }
        }

        private static void ValidateUri(string url, string name)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException($"Invalid URI: {url} ({name})");
            }
        }

        private static void ValidateFilePath(string path, string name)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"File not found: {path} ({name})");
            }
        }

        public class CommandLineParameters
        {
            [Value(0, HelpText = "The ADDS service name (e.g. 'EFS')", Required = true)]
            public string ServiceName { get; set; }

            [Value(1, HelpText = "The ADDS environment name (e.g. 'dev')", Required = true)]
            public string EnvironmentName { get; set; }

            [Value(2, HelpText = "Configuration JSON file path", Required = true)]
            public string ConfigurationFilePath { get; set; }

            [Value(3, HelpText = "Services JSON file path", Required = true)]
            public string ServicesFilePath { get; set; }

            [Value(4, HelpText = "Azure App Configuration Service URL", Required = true)]
            public string AppConfigServiceUrl { get; set; }
        }
    }
}
